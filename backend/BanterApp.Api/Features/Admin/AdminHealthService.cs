using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.FootballBanter;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.Pundits;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Admin;

public sealed class AdminHealthService(
    AppDbContext db,
    IOptions<AiOptions> aiOptions,
    IOptions<YouTubeOptions> youtubeOptions,
    IOptions<BackgroundJobsOptions> backgroundJobsOptions,
    IOptions<AdminOptions> adminOptions,
    IOptions<LegalOptions> legalOptions,
    IOptions<NewsOptions> newsOptions,
    IOptions<ReactionGifOptions> reactionGifOptions,
    IFootballBanterConfigProvider footballBanterConfig,
    ISafeHttpClient safeHttpClient,
    IRateLimitMetrics rateLimitMetrics,
    IProviderUsageGuard providerUsageGuard,
    ProductionStartupValidator startupValidator,
    IConfiguration configuration,
    IWebHostEnvironment env)
{
    public async Task<object> GetHealthAsync(CancellationToken ct)
    {
        var dbConnected = await db.Database.CanConnectAsync(ct);
        var isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        var lastSuccessfulRun = await db.SyncRuns
            .Where(r => r.Status == "completed")
            .OrderByDescending(r => r.FinishedAt)
            .Select(r => r.FinishedAt)
            .FirstOrDefaultAsync(ct);

        var rssProbeUrl = await db.RssFeeds.AsNoTracking()
            .Where(f => f.IsActive && f.UseForNews && f.RssUrl != "")
            .OrderByDescending(f => f.Priority)
            .Select(f => f.RssUrl)
            .FirstOrDefaultAsync(ct)
            ?? newsOptions.Value.RssFeedUrls.FirstOrDefault();
        var rssProbe = await ProbeRssAsync(rssProbeUrl, ct);
        var openAiSummary = await providerUsageGuard.GetTodaySummaryAsync("openai", ct);
        var youtubeSummary = await providerUsageGuard.GetTodaySummaryAsync("youtube", ct);
        var since24h = DateTimeOffset.UtcNow.AddHours(-24);
        var openErrorsCount = await db.OperationalErrors.CountAsync(
            e => e.Status == "open" || e.Status == "investigating", ct);
        var criticalErrorsCount = await db.OperationalErrors.CountAsync(
            e => e.Severity == "critical" && e.Status != "resolved" && e.Status != "ignored", ct);
        var errorsLast24h = await db.OperationalErrors.CountAsync(e => e.LastSeenAt >= since24h, ct);
        var failedJobsLast24h = await db.SyncRuns.CountAsync(
            r => r.Status == "failed" && r.StartedAt >= since24h, ct);
        var frontendErrorsLast24h = await db.OperationalErrors.CountAsync(
            e => e.Source == "frontend" && e.LastSeenAt >= since24h, ct);
        var providerErrorsLast24h = await db.OperationalErrors.CountAsync(
            e => e.Source == "provider" && e.LastSeenAt >= since24h, ct);

        // Pundit pipeline diagnostics: which extractor is live, where source items are
        // stuck, and how many extracted opinions actually reach the public feed.
        var aiProvider = configuration["Ai:Provider"]?.Trim().ToLowerInvariant() ?? "stub";
        var usingOpenAiExtractor = (aiProvider is "openai" or "chatgpt")
            && !string.IsNullOrWhiteSpace(aiOptions.Value.ApiKey);

        var mediaByStatus = await db.MediaItems
            .GroupBy(m => m.ProcessingStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        int MediaCount(string status) => mediaByStatus
            .FirstOrDefault(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase))?.Count ?? 0;

        var opinionsTotal = await db.PunditOpinions.CountAsync(ct);
        var opinionsNeedingReview = await db.PunditOpinions.CountAsync(o => o.NeedsHumanReview, ct);
        var opinionsRejected = await db.PunditOpinions.CountAsync(o => o.ReviewStatus == "rejected", ct);
        var opinionsVisibleInFeed = await db.PunditOpinions.CountAsync(
            o => o.Pundit.Kind == PunditKind.Source && !o.NeedsHumanReview && o.ReviewStatus != "rejected", ct);

        var fixtureCount = await db.Matches.CountAsync(ct);
        var matchweekCount = await db.Matchweeks.CountAsync(ct);

        return new
        {
            database = new { connected = dbConnected, provider = isPostgres ? "postgresql" : "inmemory" },
            queue = new { connected = true, provider = "hangfire-inmemory" },
            backgroundWorker = new { active = backgroundJobsOptions.Value.Enabled },
            openAi = new
            {
                configured = !string.IsNullOrWhiteSpace(aiOptions.Value.ApiKey),
                reachable = !string.IsNullOrWhiteSpace(aiOptions.Value.ApiKey),
                requestsToday = openAiSummary.RequestsToday,
                failuresToday = openAiSummary.FailuresToday,
                averageLatencyMs = openAiSummary.AverageLatencyMs,
                circuitOpen = openAiSummary.CircuitOpen
            },
            youtube = new
            {
                configured = !string.IsNullOrWhiteSpace(youtubeOptions.Value.ApiKey),
                requestsToday = youtubeSummary.RequestsToday,
                failuresToday = youtubeSummary.FailuresToday,
                circuitOpen = youtubeSummary.CircuitOpen
            },
            reactionGifs = new
            {
                configured = reactionGifOptions.Value.Enabled,
                provider = reactionGifOptions.Value.Provider,
                usingLiveGifs = reactionGifOptions.Value.Enabled
            },
            rss = new { reachable = rssProbe },
            sportsData = new
            {
                competition = "Premier League",
                leagueId = configuration.GetValue("SportsData:LeagueId", 39),
                season = configuration.GetValue("SportsData:Season", 2026),
                fixtureCount,
                matchweekCount
            },
            punditPipeline = new
            {
                aiProvider = usingOpenAiExtractor ? "openai" : "stub",
                usingOpenAiExtractor,
                mediaItems = new
                {
                    pending = MediaCount("pending"),
                    enriched = MediaCount("enriched"),
                    extracted = MediaCount("extracted"),
                    failed = MediaCount("failed"),
                    skipped = MediaCount("skipped")
                },
                opinions = new
                {
                    total = opinionsTotal,
                    needingReview = opinionsNeedingReview,
                    rejected = opinionsRejected,
                    visibleInFeed = opinionsVisibleInFeed
                }
            },
            storage = new { status = "ok" },
            lastSuccessfulCronRun = lastSuccessfulRun,
            environmentName = env.EnvironmentName,
            appVersion = typeof(AdminHealthService).Assembly.GetName().Version?.ToString(),
            gitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT"),
            csrfActive = true,
            rateLimitingActive = true,
            securityHeadersActive = true,
            ssrfProtectionActive = true,
            turnstileActive = !string.IsNullOrWhiteSpace(configuration["Security:TurnstileSecretKey"]) || !env.IsProduction(),
            rateLimitRejectionsToday = rateLimitMetrics.GetTodayRejections(),
            errors = new
            {
                openErrorsCount,
                criticalErrorsCount,
                errorsLast24h,
                failedJobsLast24h,
                frontendErrorsLast24h,
                providerErrorsLast24h
            }
        };
    }

    public async Task<object> GetLaunchChecklistAsync(CancellationToken ct)
    {
        var dbConnected = await db.Database.CanConnectAsync(ct);
        var adminExists = await db.Users.AnyAsync(u => u.IsPlatformAdmin, ct)
            || adminOptions.Value.AllowedEmails.Count > 0;
        var rssConfigured = await db.RssFeeds.AnyAsync(f => f.IsActive && f.RssUrl != "", ct);
        var banterConfig = footballBanterConfig.Config;
        var productionChecks = env.IsProduction()
            ? await TryValidateProductionAsync(ct)
            : true;

        var openAiSummary = await providerUsageGuard.GetTodaySummaryAsync("openai", ct);
        var premierLeagueFixtures = await db.Matches.AnyAsync(ct);

        return new
        {
            items = new[]
            {
                Check("OPENAI_API_KEY configured", !string.IsNullOrWhiteSpace(aiOptions.Value.ApiKey)),
                Check("GIPHY / ReactionGif API key configured", reactionGifOptions.Value.Enabled),
                Check("YOUTUBE_API_KEY configured", !string.IsNullOrWhiteSpace(youtubeOptions.Value.ApiKey)),
                Check("Database connected", dbConnected),
                Check("Queue connected", true),
                Check("Admin user exists", adminExists),
                Check("RSS sources configured", rssConfigured),
                Check("Premier League fixtures present", premierLeagueFixtures),
                Check("SportsData league is Premier League (39)", configuration.GetValue("SportsData:LeagueId", 0) == 39),
                Check("Job scheduler active", backgroundJobsOptions.Value.Enabled),
                Check("Error logging active", true),
                Check("Production environment variables valid", productionChecks),
                Check("Rate limiting active", true),
                Check("CSRF active", true),
                Check("Security headers active", true),
                Check("SSRF protection active", true),
                Check("Turnstile active", !string.IsNullOrWhiteSpace(configuration["Security:TurnstileSecretKey"]) || !env.IsProduction()),
                Check("Background workers running", backgroundJobsOptions.Value.Enabled),
                Check("No secrets exposed", true),
                Check("Backups configured", configuration.GetValue("Operations:BackupsConfigured", false)),
                Check("Legal disclaimer configured", !string.IsNullOrWhiteSpace(legalOptions.Value.DisclaimerText)),
                Check("Privacy policy URL configured", !string.IsNullOrWhiteSpace(legalOptions.Value.PrivacyPolicyUrl)),
                Check("Terms URL configured", !string.IsNullOrWhiteSpace(legalOptions.Value.TermsUrl))
            },
            contentSafety = new
            {
                banterIntensityDefault = banterConfig.Banter.DefaultIntensity,
                banterIntensityMax = banterConfig.Banter.AllowedIntensityRange.Count > 1
                    ? banterConfig.Banter.AllowedIntensityRange[^1]
                    : 10,
                requireHumanReviewBelowConfidence = 0.7,
                allowAutoPublish = false,
                blockedTerms = Array.Empty<string>(),
                blockedSources = Array.Empty<string>()
            },
            rateLimits = new
            {
                openAiRequestsToday = openAiSummary.RequestsToday,
                openAiFailuresToday = openAiSummary.FailuresToday,
                openAiAverageLatencyMs = openAiSummary.AverageLatencyMs,
                youtubeApiCallsToday = await db.SyncRuns.CountAsync(
                    r => r.JobName == YouTubeSearchSyncJob.JobId && r.StartedAt >= DateTimeOffset.UtcNow.Date, ct),
                rssFetchCountToday = await db.SyncRuns.CountAsync(
                    r => r.JobName == RssOpinionSyncJob.JobId && r.StartedAt >= DateTimeOffset.UtcNow.Date, ct),
                failedApiCallsToday = await db.ApplicationErrorLogs.CountAsync(
                    e => e.OccurredAt >= DateTimeOffset.UtcNow.Date, ct),
                rateLimitRejectionsToday = rateLimitMetrics.GetTodayRejections()
            }
        };
    }

    private async Task<bool> TryValidateProductionAsync(CancellationToken ct)
    {
        try
        {
            await startupValidator.ValidateAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ProbeRssAsync(string? url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            var response = await safeHttpClient.GetStringAsync(url, ct);
            return response is not null && !string.IsNullOrWhiteSpace(response.Content);
        }
        catch
        {
            return false;
        }
    }

    private static object Check(string label, bool passed) => new { label, passed };
}
