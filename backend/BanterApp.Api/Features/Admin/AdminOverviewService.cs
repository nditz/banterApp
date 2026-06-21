using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Jobs;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Pundits;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Admin;

public sealed class LegalOptions
{
    public const string SectionName = "Legal";
    public string PrivacyPolicyUrl { get; set; } = string.Empty;
    public string TermsUrl { get; set; } = string.Empty;
    public string DisclaimerText { get; set; } = string.Empty;
}

public sealed class AdminOverviewService(
    AppDbContext db,
    IOptions<AiOptions> aiOptions,
    IOptions<BackgroundJobsOptions> backgroundJobsOptions)
{
    public async Task<object> GetOverviewAsync(CancellationToken ct)
    {
        var since24h = DateTimeOffset.UtcNow.AddHours(-24);
        var today = DateTimeOffset.UtcNow.Date;

        var rssCount = await db.MediaItems.CountAsync(
            i => i.MediaSource.SourceType == "rss", ct);
        var youtubeCount = await db.MediaItems.CountAsync(
            i => i.MediaSource.SourceType == "youtube", ct);
        var opinionsCount = await db.PunditOpinions.CountAsync(ct);
        var predictionsCount = await db.PredictionAggregates.CountAsync(ct);
        var needsReview = await db.PunditOpinions.CountAsync(
            o => o.NeedsHumanReview && o.ReviewStatus == "pending", ct);
        var failedJobs24h = await db.SyncRuns.CountAsync(
            r => r.Status == "failed" && r.StartedAt >= since24h, ct);
        var openAiRequests24h = await db.ApplicationErrorLogs
            .Where(e => e.OccurredAt >= since24h)
            .Select(e => e.Category)
            .ToListAsync(ct);
        var openAiRequestCount = openAiRequests24h.Count(c =>
            c.Contains("openai", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("pundit-extraction", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("feed-banter", StringComparison.OrdinalIgnoreCase));
        var latestSuccess = await db.SyncRuns
            .Where(r => r.Status == "completed")
            .OrderByDescending(r => r.FinishedAt)
            .FirstOrDefaultAsync(ct);
        var latestFailure = await db.SyncRuns
            .Where(r => r.Status == "failed")
            .OrderByDescending(r => r.FinishedAt)
            .FirstOrDefaultAsync(ct);

        return new
        {
            totalRssItems = rssCount,
            totalYoutubeItems = youtubeCount,
            totalOpinions = opinionsCount,
            totalPredictions = predictionsCount,
            itemsNeedingReview = needsReview,
            failedJobsLast24h = failedJobs24h,
            openAiRequestsLast24h = openAiRequestCount,
            youtubeQuotaAvailable = false,
            latestSuccessfulSyncAt = latestSuccess?.FinishedAt,
            latestFailedSyncAt = latestFailure?.FinishedAt,
            jobsEnabled = backgroundJobsOptions.Value.Enabled,
            openAiConfigured = !string.IsNullOrWhiteSpace(aiOptions.Value.ApiKey)
        };
    }

    public async Task<object> GetStatsAsync(CancellationToken ct)
    {
        var since24h = DateTimeOffset.UtcNow.AddHours(-24);
        var today = DateTimeOffset.UtcNow.Date;
        var totalUsers = await db.Users.CountAsync(ct);
        var rssToday = await db.MediaItems.CountAsync(i => i.LastSyncedAt >= today, ct);
        var apiErrorsToday = await db.OperationalErrors.CountAsync(e => e.LastSeenAt >= today, ct);
        var openErrorsCount = await db.OperationalErrors.CountAsync(
            e => e.Status == "open" || e.Status == "investigating", ct);
        var criticalErrorsCount = await db.OperationalErrors.CountAsync(
            e => e.Severity == "critical" && e.Status != "resolved" && e.Status != "ignored", ct);
        var errorsLast24h = await db.OperationalErrors.CountAsync(e => e.LastSeenAt >= since24h, ct);
        var frontendErrorsLast24h = await db.OperationalErrors.CountAsync(
            e => e.Source == "frontend" && e.LastSeenAt >= since24h, ct);
        var providerErrorsLast24h = await db.OperationalErrors.CountAsync(
            e => e.Source == "provider" && e.LastSeenAt >= since24h, ct);
        var failedJobsLast24h = await db.SyncRuns.CountAsync(
            r => r.Status == "failed" && r.StartedAt >= since24h, ct);
        var openAiErrorsToday = await db.OperationalErrors.CountAsync(
            e => e.Provider == "openai" && e.LastSeenAt >= today, ct);
        var failedQueueItems = await db.MediaItems.CountAsync(
            i => i.ProcessingStatus == MediaItemProcessingStatus.Failed, ct);

        var topTeams = await db.PunditOpinions
            .Where(o => o.Team != null)
            .GroupBy(o => o.Team!)
            .Select(g => new { name = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(10)
            .ToListAsync(ct);

        var topPundits = await db.PunditOpinions
            .Include(o => o.Pundit)
            .GroupBy(o => o.Pundit.Name)
            .Select(g => new { name = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(10)
            .ToListAsync(ct);

        var topPublications = await db.MediaItems
            .Where(i => i.Publication != null)
            .GroupBy(i => i.Publication!)
            .Select(g => new { name = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(10)
            .ToListAsync(ct);

        return new
        {
            product = new
            {
                dailyActiveUsers = new { available = false, metricKey = "daily_active_users" },
                totalUsers,
                pageViews = new { available = false, metricKey = "page_views" },
                feedImpressions = new { available = false, metricKey = "feed_impressions" },
                feedClicks = new { available = false, metricKey = "feed_clicks" },
                shares = new { available = false, metricKey = "shares" },
                savedItems = new { available = false, metricKey = "saved_items" },
                commentsReactions = new { available = false, metricKey = "comments_reactions" }
            },
            backend = new
            {
                apiErrorRateToday = apiErrorsToday,
                openAiFailureCountToday = openAiErrorsToday,
                openErrorsCount,
                criticalErrorsCount,
                errorsLast24h,
                failedJobsLast24h,
                frontendErrorsLast24h,
                providerErrorsLast24h,
                rssItemsFetchedToday = rssToday,
                youtubeVideosFetchedToday = await db.MediaItems.CountAsync(
                    i => i.LastSyncedAt >= today && i.MediaSource.SourceType == "youtube", ct),
                queueDepth = await db.MediaItems.CountAsync(
                    i => i.ProcessingStatus == MediaItemProcessingStatus.Pending, ct),
                failedQueueItems
            },
            topTeams,
            topPundits,
            topPublications,
            topArticles = await db.MediaItems
                .OrderByDescending(i => i.LastSyncedAt)
                .Take(10)
                .Select(i => new { i.Id, i.Title, i.Publication, i.LastSyncedAt })
                .ToListAsync(ct),
            topBanterPosts = await db.NewsFeedItems
                .OrderByDescending(n => n.ViewCount)
                .Take(10)
                .Select(n => new { n.Id, n.Title, n.ViewCount })
                .ToListAsync(ct)
        };
    }
}
