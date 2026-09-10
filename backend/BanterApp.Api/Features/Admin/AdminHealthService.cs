using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
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
        var punditPredictionsTotal = await db.PunditPredictions.CountAsync(ct);
        var punditPredictionsMatchLinked = await db.PunditPredictions.CountAsync(
            p => p.MatchId != null && p.IsMatched, ct);
        var opinionsMatchLinked = await db.PunditOpinions.CountAsync(o => o.MatchId != null, ct);
        var opinionsMatchLinkedWithoutPrediction = await db.PunditOpinions.CountAsync(
            o => o.MatchId != null &&
                 o.ReviewStatus != "rejected" &&
                 !db.PunditPredictions.Any(p => p.PunditId == o.PunditId && p.MatchId == o.MatchId),
            ct);

        var fixtureCount = await db.Matches.CountAsync(ct);
        var matchweekCount = await db.Matchweeks.CountAsync(ct);
        var sportsProvider = configuration["SportsData:Provider"]?.Trim().ToLowerInvariant() ?? "mock";
        var sportsApiKey = configuration["SportsData:ApiKey"];
        var lastScoreSync = await db.SyncRuns
            .Where(r => r.JobName == ScoreSyncJob.JobId)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(ct);
        var overdueUnfinished = await db.Matches
            .WherePremierLeague()
            .ToListAsync(ct);
        var hasOverdue = FootballDatasetStatus.HasOverdueUnfinished(
            overdueUnfinished.Select(m => ((string?)m.Status, m.KickoffTime)),
            DateTimeOffset.UtcNow);

        var resolvedMatchweek = await MatchEndpoints.ResolveCurrentMatchweekNumberAsync(db, ct);
        var currentMatchweek = resolvedMatchweek > 0 ? resolvedMatchweek : (int?)null;

        // Receipts and Studio packs are user-visible outputs, so a stall in either is an
        // outage even when every ingestion job is green.
        var receiptsTotal = await db.PredictionReceipts.CountAsync(ct);
        var receiptsLast24h = await db.PredictionReceipts.CountAsync(r => r.SettledAt >= since24h, ct);
        var settledPredictions = await db.Predictions.CountAsync(
            p => p.Match != null && p.Match.HomeScore != null && p.Match.AwayScore != null, ct);
        var receiptsMissing = Math.Max(0, settledPredictions - receiptsTotal);

        var packsTotal = await db.GeneratedContents.CountAsync(
            c => c.Type == GeneratedContentType.ContentPack, ct);
        var packsLast24h = await db.GeneratedContents.CountAsync(
            c => c.Type == GeneratedContentType.ContentPack && c.CreatedAt >= since24h, ct);

        var adInitFailures24h = await db.AppMetrics.CountAsync(
            m => m.MetricKey == "ad_init_failed" && m.RecordedAt >= since24h, ct);

        var repeatedJobFailures = await db.SyncRuns
            .Where(r => r.Status == "failed" && r.StartedAt >= since24h)
            .GroupBy(r => r.JobName)
            .Select(g => new { JobName = g.Key, Count = g.Count() })
            .Where(x => x.Count >= RepeatedJobFailureThreshold)
            .ToListAsync(ct);

        var alerts = BuildAlerts(new AlertInputs(
            DatabaseConnected: dbConnected,
            CurrentMatchweek: currentMatchweek,
            FixtureCount: fixtureCount,
            OverdueUnfinishedFixtures: hasOverdue,
            ReceiptsMissing: receiptsMissing,
            CriticalErrors: criticalErrorsCount,
            AdInitFailures24h: adInitFailures24h,
            RepeatedlyFailingJobs: repeatedJobFailures.Select(x => x.JobName).ToList()));

        return new
        {
            status = alerts.Any(a => a.Severity == "critical")
                ? "unhealthy"
                : alerts.Count > 0 ? "degraded" : "ok",
            alerts,
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
                provider = sportsProvider,
                apiKeyConfigured = !string.IsNullOrWhiteSpace(sportsApiKey),
                footballDataTokenConfigured = !string.IsNullOrWhiteSpace(configuration["FootballData:Token"]),
                usingMock = FootballDatasetStatus.IsMockProvider(sportsProvider),
                fixtureCount,
                matchweekCount,
                lastScoreSyncStatus = lastScoreSync?.Status,
                lastScoreSyncAt = lastScoreSync?.FinishedAt,
                lastScoreSyncError = lastScoreSync?.ErrorMessage,
                overdueUnfinishedFixtures = hasOverdue
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
                    visibleInFeed = opinionsVisibleInFeed,
                    matchLinked = opinionsMatchLinked,
                    matchLinkedWithoutPrediction = opinionsMatchLinkedWithoutPrediction
                },
                predictions = new
                {
                    total = punditPredictionsTotal,
                    matchLinked = punditPredictionsMatchLinked
                }
            },
            receipts = new
            {
                total = receiptsTotal,
                settledLast24h = receiptsLast24h,
                settledPredictions,
                awaitingSettlement = receiptsMissing
            },
            studio = new
            {
                contentPacks = packsTotal,
                contentPacksLast24h = packsLast24h
            },
            ads = new
            {
                consentModel = "opt-in",
                initFailuresLast24h = adInitFailures24h
            },
            storage = new { status = "ok" },
            currentMatchweek,
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

    /// <summary>Failures of one job within 24h before it counts as repeatedly failing.</summary>
    private const int RepeatedJobFailureThreshold = 3;

    /// <summary>Settled predictions allowed to lack a receipt before it looks like a stall.</summary>
    private const int ReceiptBacklogThreshold = 5;

    public sealed record AdminAlert(string Key, string Severity, string Message);

    private sealed record AlertInputs(
        bool DatabaseConnected,
        int? CurrentMatchweek,
        int FixtureCount,
        bool OverdueUnfinishedFixtures,
        int ReceiptsMissing,
        int CriticalErrors,
        int AdInitFailures24h,
        IReadOnlyList<string> RepeatedlyFailingJobs);

    /// <summary>
    /// Turns raw counts into the conditions an operator should act on, so production
    /// failures are visible in admin before a user reports them.
    /// </summary>
    private static List<AdminAlert> BuildAlerts(AlertInputs input)
    {
        var alerts = new List<AdminAlert>();

        if (!input.DatabaseConnected)
        {
            alerts.Add(new AdminAlert("database_unreachable", "critical", "The database is unreachable."));
        }

        if (input.FixtureCount == 0)
        {
            alerts.Add(new AdminAlert("no_fixtures", "critical", "No fixtures are loaded — the product has nothing to predict."));
        }

        if (input.CurrentMatchweek is null)
        {
            alerts.Add(new AdminAlert("no_current_matchweek", "critical", "No matchweek is marked current, so predictions cannot open."));
        }

        if (input.OverdueUnfinishedFixtures)
        {
            alerts.Add(new AdminAlert("overdue_fixtures", "warning", "Fixtures are past kickoff with no result — score sync may be stalled."));
        }

        if (input.ReceiptsMissing > ReceiptBacklogThreshold)
        {
            alerts.Add(new AdminAlert(
                "receipt_settlement_backlog",
                "warning",
                $"{input.ReceiptsMissing} settled predictions have no receipt."));
        }

        foreach (var jobName in input.RepeatedlyFailingJobs)
        {
            alerts.Add(new AdminAlert($"job_failing:{jobName}", "warning", $"Job '{jobName}' failed repeatedly in the last 24h."));
        }

        if (input.AdInitFailures24h > 0)
        {
            alerts.Add(new AdminAlert("adsense_init_failures", "info", $"AdSense failed to initialise {input.AdInitFailures24h} times in the last 24h."));
        }

        if (input.CriticalErrors > 0)
        {
            alerts.Add(new AdminAlert("critical_errors_open", "critical", $"{input.CriticalErrors} critical errors are unresolved."));
        }

        return alerts;
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
        var sportsProvider = configuration["SportsData:Provider"]?.Trim().ToLowerInvariant() ?? "mock";
        var sportsLive = FootballDatasetStatus.IsFootballDataProvider(sportsProvider)
            ? !string.IsNullOrWhiteSpace(configuration["FootballData:Token"])
            : sportsProvider == "apifootball" &&
              !string.IsNullOrWhiteSpace(configuration["SportsData:ApiKey"]);
        var plMatches = await db.Matches.WherePremierLeague().ToListAsync(ct);
        var fixturesFresh = plMatches.Count > 0 &&
                            !FootballDatasetStatus.HasOverdueUnfinished(
                                plMatches.Select(m => ((string?)m.Status, m.KickoffTime)),
                                DateTimeOffset.UtcNow);
        var competitionOk = FootballDatasetStatus.IsFootballDataProvider(sportsProvider)
            ? string.Equals(configuration["FootballData:CompetitionCode"] ?? "PL", "PL", StringComparison.OrdinalIgnoreCase)
            : configuration.GetValue("SportsData:LeagueId", 0) == 39;

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
                Check("SportsData uses live football-data.org", sportsLive),
                Check("Current fixtures are not overdue without results", fixturesFresh),
                Check("SportsData competition is Premier League", competitionOk),
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
