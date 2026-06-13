using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.SportsData;
using Hangfire;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations;

public static class HangfireJobRegistration
{
    public static void RegisterRecurringJobs(WebApplication app)
    {
        var jobs = app.Services.GetRequiredService<IOptions<BackgroundJobsOptions>>().Value;
        if (!jobs.Enabled)
        {
            return;
        }

        var recurring = app.Services.GetRequiredService<IRecurringJobManager>();

        var liveInterval = Math.Clamp(jobs.LiveScoresIntervalMinutes, 1, 59);
        recurring.AddOrUpdate<ScoreSyncJob>(
            ScoreSyncJob.JobId,
            job => job.SyncAsync(CancellationToken.None),
            $"*/{liveInterval} * * * *");

        var detailsInterval = Math.Clamp(jobs.MatchDetailsIntervalMinutes, 1, 59);
        recurring.AddOrUpdate<MatchDetailsSyncJob>(
            MatchDetailsSyncJob.JobId,
            job => job.SyncAsync(CancellationToken.None),
            $"*/{detailsInterval} * * * *");

        var standingsInterval = Math.Clamp(jobs.StandingsIntervalMinutes, 30, 1440);
        var standingsCron = BuildStaggeredCron(standingsInterval, jobs.StandingsStartMinute);
        recurring.AddOrUpdate<StandingsSyncJob>(
            StandingsSyncJob.JobId,
            job => job.SyncAsync(CancellationToken.None),
            standingsCron);

        var aiInterval = Math.Clamp(jobs.AiReactionsIntervalMinutes, 5, 120);
        var aiCron = BuildStaggeredCron(aiInterval, jobs.AiReactionsStartMinute);
        recurring.AddOrUpdate<AiReactionJob>(
            AiReactionJob.JobId,
            job => job.ReactAsync(CancellationToken.None),
            aiCron);

        var newsInterval = Math.Clamp(jobs.NewsIngestIntervalMinutes, 30, 1440);
        var newsCron = BuildStaggeredCron(newsInterval, jobs.NewsIngestStartMinute);
        recurring.AddOrUpdate<NewsIngestJob>(
            NewsIngestJob.JobId,
            job => job.IngestAsync(CancellationToken.None),
            newsCron);

        var mediaInterval = Math.Clamp(jobs.MediaIngestIntervalMinutes, 60, 1440);
        var mediaCron = BuildStaggeredCron(mediaInterval, jobs.MediaIngestStartMinute);
        recurring.AddOrUpdate<MediaIngestJob>(
            MediaIngestJob.JobId,
            job => job.IngestAsync(CancellationToken.None),
            mediaCron);

        recurring.Trigger(ScoreSyncJob.JobId);
        recurring.Trigger(StandingsSyncJob.JobId);
        recurring.Trigger(NewsIngestJob.JobId);
        recurring.Trigger(MediaIngestJob.JobId);
        recurring.Trigger(AiReactionJob.JobId);
    }

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
