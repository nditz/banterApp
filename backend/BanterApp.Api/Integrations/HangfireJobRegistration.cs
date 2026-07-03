using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.Pundits;
using BanterApp.Api.Integrations.FootballReference.Jobs;
using BanterApp.Api.Integrations.SportsData;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations;

public static class HangfireJobRegistration
{
    private static readonly string[] AllJobIds =
    [
        ScoreSyncJob.JobId,
        MatchDetailsSyncJob.JobId,
        StandingsSyncJob.JobId,
        AiReactionJob.JobId,
        NewsIngestJob.JobId,
        MediaIngestJob.JobId,
        RssOpinionSyncJob.JobId,
        YouTubeSearchSyncJob.JobId,
        ContentEnrichmentJob.JobId,
        FeedBanterEnrichmentJob.JobId,
        PunditExtractionJob.JobId,
        PredictionAggregateJob.JobId,
        FootballCountriesSyncJob.JobId,
        FootballPlayersSyncJob.JobId,
        FootballPlayerStatsSyncJob.JobId,
        FootballTopScorersSyncJob.JobId,
        FootballTopAssistsSyncJob.JobId,
        FootballReferenceFullSyncJob.JobId
    ];

    public static void RegisterRecurringJobs(WebApplication app)
    {
        var jobs = app.Services.GetRequiredService<IOptions<BackgroundJobsOptions>>().Value;
        if (!jobs.Enabled)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pausedOrDisabled = db.JobRegistryStates.AsNoTracking()
            .Where(s => !s.Enabled || s.Paused)
            .Select(s => s.JobKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var recurring = app.Services.GetRequiredService<IRecurringJobManager>();

        RegisterAllJobs(recurring, jobs, pausedOrDisabled);

        recurring.Trigger(ScoreSyncJob.JobId);
        recurring.Trigger(StandingsSyncJob.JobId);
        recurring.Trigger(NewsIngestJob.JobId);
        recurring.Trigger(MediaIngestJob.JobId);
        recurring.Trigger(AiReactionJob.JobId);
        recurring.Trigger(FeedBanterEnrichmentJob.JobId);
        recurring.Trigger(RssOpinionSyncJob.JobId);
        recurring.Trigger(ContentEnrichmentJob.JobId);
        recurring.Trigger(PunditExtractionJob.JobId);
    }

    public static void RegisterSingleJob(IRecurringJobManager recurring, string hangfireJobId, BackgroundJobsOptions jobs)
    {
        RegisterJobById(recurring, hangfireJobId, jobs);
    }

    private static void RegisterAllJobs(
        IRecurringJobManager recurring,
        BackgroundJobsOptions jobs,
        HashSet<string> pausedOrDisabled)
    {
        if (!IsPaused(pausedOrDisabled, "score-sync"))
        {
            var liveInterval = Math.Clamp(jobs.LiveScoresIntervalMinutes, 1, 59);
            recurring.AddOrUpdate<ScoreSyncJob>(
                ScoreSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                $"*/{liveInterval} * * * *");
        }

        if (!IsPaused(pausedOrDisabled, "match-details-sync"))
        {
            var detailsInterval = Math.Clamp(jobs.MatchDetailsIntervalMinutes, 1, 59);
            recurring.AddOrUpdate<MatchDetailsSyncJob>(
                MatchDetailsSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                $"*/{detailsInterval} * * * *");
        }

        if (!IsPaused(pausedOrDisabled, "standings-sync"))
        {
            var standingsInterval = Math.Clamp(jobs.StandingsIntervalMinutes, 30, 1440);
            var standingsCron = BuildStaggeredCron(standingsInterval, jobs.StandingsStartMinute);
            recurring.AddOrUpdate<StandingsSyncJob>(
                StandingsSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                standingsCron);
        }

        if (!IsPaused(pausedOrDisabled, "ai-reactions"))
        {
            var aiInterval = Math.Clamp(jobs.AiReactionsIntervalMinutes, 5, 120);
            var aiCron = BuildStaggeredCron(aiInterval, jobs.AiReactionsStartMinute);
            recurring.AddOrUpdate<AiReactionJob>(
                AiReactionJob.JobId,
                job => job.ReactAsync(CancellationToken.None),
                aiCron);
        }

        if (!IsPaused(pausedOrDisabled, "news-ingest"))
        {
            var newsInterval = Math.Clamp(jobs.NewsIngestIntervalMinutes, 30, 1440);
            var newsCron = BuildStaggeredCron(newsInterval, jobs.NewsIngestStartMinute);
            recurring.AddOrUpdate<NewsIngestJob>(
                NewsIngestJob.JobId,
                job => job.IngestAsync(CancellationToken.None),
                newsCron);
        }

        if (!IsPaused(pausedOrDisabled, "youtube.metadata.sync"))
        {
            var mediaInterval = Math.Clamp(jobs.MediaIngestIntervalMinutes, 60, 1440);
            var mediaCron = BuildStaggeredCron(mediaInterval, jobs.MediaIngestStartMinute);
            recurring.AddOrUpdate<MediaIngestJob>(
                MediaIngestJob.JobId,
                job => job.IngestAsync(CancellationToken.None),
                mediaCron);
        }

        if (!IsPaused(pausedOrDisabled, "rss.sync"))
        {
            var rssOpinionInterval = Math.Clamp(jobs.RssOpinionSyncIntervalMinutes, 15, 60);
            var rssOpinionCron = BuildStaggeredCron(rssOpinionInterval, jobs.RssOpinionSyncStartMinute);
            recurring.AddOrUpdate<RssOpinionSyncJob>(
                RssOpinionSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                rssOpinionCron);
        }

        if (!IsPaused(pausedOrDisabled, "youtube.search.sync"))
        {
            var youtubeSearchInterval = Math.Clamp(jobs.YouTubeSearchSyncIntervalMinutes, 60, 360);
            var youtubeSearchCron = BuildStaggeredCron(youtubeSearchInterval, jobs.YouTubeSearchSyncStartMinute);
            recurring.AddOrUpdate<YouTubeSearchSyncJob>(
                YouTubeSearchSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                youtubeSearchCron);
        }

        if (!IsPaused(pausedOrDisabled, "youtube.transcript.sync"))
        {
            var enrichInterval = Math.Clamp(jobs.PunditContentEnrichIntervalMinutes, 5, 60);
            var enrichCron = BuildStaggeredCron(enrichInterval, jobs.PunditContentEnrichStartMinute);
            recurring.AddOrUpdate<ContentEnrichmentJob>(
                ContentEnrichmentJob.JobId,
                job => job.EnrichAsync(CancellationToken.None),
                enrichCron);
        }

        if (!IsPaused(pausedOrDisabled, "openai.banter.generate"))
        {
            var banterInterval = Math.Clamp(jobs.FeedBanterEnrichmentIntervalMinutes, 5, 120);
            var banterCron = BuildStaggeredCron(banterInterval, jobs.FeedBanterEnrichmentStartMinute);
            recurring.AddOrUpdate<FeedBanterEnrichmentJob>(
                FeedBanterEnrichmentJob.JobId,
                job => job.EnrichAsync(CancellationToken.None),
                banterCron);
        }

        if (!IsPaused(pausedOrDisabled, "openai.opinion.extract"))
        {
            var extractionInterval = Math.Clamp(jobs.PunditExtractionIntervalMinutes, 5, 60);
            var extractionCron = BuildStaggeredCron(extractionInterval, jobs.PunditExtractionStartMinute);
            recurring.AddOrUpdate<PunditExtractionJob>(
                PunditExtractionJob.JobId,
                job => job.ExtractAsync(CancellationToken.None),
                extractionCron);
        }

        recurring.AddOrUpdate<PredictionAggregateJob>(
            PredictionAggregateJob.JobId,
            job => job.RefreshAsync(CancellationToken.None),
            Cron.Never());

        if (!IsPaused(pausedOrDisabled, "football.countries.sync"))
        {
            recurring.AddOrUpdate<FootballCountriesSyncJob>(
                FootballCountriesSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                "0 4 * * *");
        }

        if (!IsPaused(pausedOrDisabled, "football.players.sync"))
        {
            recurring.AddOrUpdate<FootballPlayersSyncJob>(
                FootballPlayersSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                "0 5 * * *");
        }

        if (!IsPaused(pausedOrDisabled, "football.player_stats.sync"))
        {
            recurring.AddOrUpdate<FootballPlayerStatsSyncJob>(
                FootballPlayerStatsSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                "0 6 * * *");
        }

        if (!IsPaused(pausedOrDisabled, "football.top_scorers.sync"))
        {
            recurring.AddOrUpdate<FootballTopScorersSyncJob>(
                FootballTopScorersSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                "*/30 * * * *");
        }

        if (!IsPaused(pausedOrDisabled, "football.top_assists.sync"))
        {
            recurring.AddOrUpdate<FootballTopAssistsSyncJob>(
                FootballTopAssistsSyncJob.JobId,
                job => job.SyncAsync(CancellationToken.None),
                "*/30 * * * *");
        }

        recurring.AddOrUpdate<FootballReferenceFullSyncJob>(
            FootballReferenceFullSyncJob.JobId,
            job => job.SyncAsync(CancellationToken.None),
            Cron.Never());
    }

    private static void RegisterJobById(IRecurringJobManager recurring, string hangfireJobId, BackgroundJobsOptions jobs)
    {
        switch (hangfireJobId)
        {
            case ScoreSyncJob.JobId:
                recurring.AddOrUpdate<ScoreSyncJob>(ScoreSyncJob.JobId, j => j.SyncAsync(CancellationToken.None),
                    $"*/{Math.Clamp(jobs.LiveScoresIntervalMinutes, 1, 59)} * * * *");
                break;
            case MatchDetailsSyncJob.JobId:
                recurring.AddOrUpdate<MatchDetailsSyncJob>(MatchDetailsSyncJob.JobId, j => j.SyncAsync(CancellationToken.None),
                    $"*/{Math.Clamp(jobs.MatchDetailsIntervalMinutes, 1, 59)} * * * *");
                break;
            case StandingsSyncJob.JobId:
                recurring.AddOrUpdate<StandingsSyncJob>(StandingsSyncJob.JobId, j => j.SyncAsync(CancellationToken.None),
                    BuildStaggeredCron(Math.Clamp(jobs.StandingsIntervalMinutes, 30, 1440), jobs.StandingsStartMinute));
                break;
            case AiReactionJob.JobId:
                recurring.AddOrUpdate<AiReactionJob>(AiReactionJob.JobId, j => j.ReactAsync(CancellationToken.None),
                    BuildStaggeredCron(Math.Clamp(jobs.AiReactionsIntervalMinutes, 5, 120), jobs.AiReactionsStartMinute));
                break;
            case NewsIngestJob.JobId:
                recurring.AddOrUpdate<NewsIngestJob>(NewsIngestJob.JobId, j => j.IngestAsync(CancellationToken.None),
                    BuildStaggeredCron(Math.Clamp(jobs.NewsIngestIntervalMinutes, 30, 1440), jobs.NewsIngestStartMinute));
                break;
            case MediaIngestJob.JobId:
                recurring.AddOrUpdate<MediaIngestJob>(MediaIngestJob.JobId, j => j.IngestAsync(CancellationToken.None),
                    BuildStaggeredCron(Math.Clamp(jobs.MediaIngestIntervalMinutes, 60, 1440), jobs.MediaIngestStartMinute));
                break;
            case RssOpinionSyncJob.JobId:
                recurring.AddOrUpdate<RssOpinionSyncJob>(RssOpinionSyncJob.JobId, j => j.SyncAsync(CancellationToken.None),
                    BuildStaggeredCron(Math.Clamp(jobs.RssOpinionSyncIntervalMinutes, 15, 60), jobs.RssOpinionSyncStartMinute));
                break;
            case YouTubeSearchSyncJob.JobId:
                recurring.AddOrUpdate<YouTubeSearchSyncJob>(YouTubeSearchSyncJob.JobId, j => j.SyncAsync(CancellationToken.None),
                    BuildStaggeredCron(Math.Clamp(jobs.YouTubeSearchSyncIntervalMinutes, 60, 360), jobs.YouTubeSearchSyncStartMinute));
                break;
            case ContentEnrichmentJob.JobId:
                recurring.AddOrUpdate<ContentEnrichmentJob>(ContentEnrichmentJob.JobId, j => j.EnrichAsync(CancellationToken.None),
                    BuildStaggeredCron(Math.Clamp(jobs.PunditContentEnrichIntervalMinutes, 5, 60), jobs.PunditContentEnrichStartMinute));
                break;
            case FeedBanterEnrichmentJob.JobId:
                recurring.AddOrUpdate<FeedBanterEnrichmentJob>(FeedBanterEnrichmentJob.JobId, j => j.EnrichAsync(CancellationToken.None),
                    BuildStaggeredCron(Math.Clamp(jobs.FeedBanterEnrichmentIntervalMinutes, 5, 120), jobs.FeedBanterEnrichmentStartMinute));
                break;
            case PunditExtractionJob.JobId:
                recurring.AddOrUpdate<PunditExtractionJob>(PunditExtractionJob.JobId, j => j.ExtractAsync(CancellationToken.None),
                    BuildStaggeredCron(Math.Clamp(jobs.PunditExtractionIntervalMinutes, 5, 60), jobs.PunditExtractionStartMinute));
                break;
            case PredictionAggregateJob.JobId:
                recurring.AddOrUpdate<PredictionAggregateJob>(PredictionAggregateJob.JobId, j => j.RefreshAsync(CancellationToken.None), Cron.Never());
                break;
            case FootballCountriesSyncJob.JobId:
                recurring.AddOrUpdate<FootballCountriesSyncJob>(FootballCountriesSyncJob.JobId, j => j.SyncAsync(CancellationToken.None), "0 4 * * *");
                break;
            case FootballPlayersSyncJob.JobId:
                recurring.AddOrUpdate<FootballPlayersSyncJob>(FootballPlayersSyncJob.JobId, j => j.SyncAsync(CancellationToken.None), "0 5 * * *");
                break;
            case FootballPlayerStatsSyncJob.JobId:
                recurring.AddOrUpdate<FootballPlayerStatsSyncJob>(FootballPlayerStatsSyncJob.JobId, j => j.SyncAsync(CancellationToken.None), "0 6 * * *");
                break;
            case FootballTopScorersSyncJob.JobId:
                recurring.AddOrUpdate<FootballTopScorersSyncJob>(FootballTopScorersSyncJob.JobId, j => j.SyncAsync(CancellationToken.None), "*/30 * * * *");
                break;
            case FootballTopAssistsSyncJob.JobId:
                recurring.AddOrUpdate<FootballTopAssistsSyncJob>(FootballTopAssistsSyncJob.JobId, j => j.SyncAsync(CancellationToken.None), "*/30 * * * *");
                break;
            case FootballReferenceFullSyncJob.JobId:
                recurring.AddOrUpdate<FootballReferenceFullSyncJob>(FootballReferenceFullSyncJob.JobId, j => j.SyncAsync(CancellationToken.None), Cron.Never());
                break;
        }
    }

    private static bool IsPaused(HashSet<string> pausedOrDisabled, string registryKey) =>
        pausedOrDisabled.Contains(registryKey);

    internal static string BuildStaggeredCron(int intervalMinutes, int startMinute)
    {
        startMinute = Math.Clamp(startMinute, 0, 59);

        if (intervalMinutes >= 60)
        {
            var hourStep = Math.Max(1, intervalMinutes / 60);
            return $"{startMinute} */{hourStep} * * *";
        }

        var minutes = new List<int>();
        for (var m = startMinute; m < 60; m += intervalMinutes)
        {
            minutes.Add(m);
        }

        return minutes.Count > 0
            ? $"{string.Join(',', minutes)} * * * *"
            : $"{startMinute} * * * *";
    }
}
