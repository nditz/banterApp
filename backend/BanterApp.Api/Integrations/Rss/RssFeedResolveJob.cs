using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.Pundits;
using Hangfire;

namespace BanterApp.Api.Integrations.Rss;

public sealed class RssFeedResolveJob(
    IRssFeedCatalog catalog,
    RssFeedResolver resolver,
    SyncRunTracker tracker,
    IBackgroundJobClient backgroundJobs,
    ILogger<RssFeedResolveJob> logger)
{
    public const string JobId = "rss-feed-resolve";
    private const string Provider = "rss-catalog";

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(60 * 30)]
    public async Task ResolveAsync(CancellationToken cancellationToken)
    {
        var run = await tracker.StartAsync(Provider, JobId, cancellationToken);

        try
        {
            await catalog.SeedAsync(cancellationToken);
            var result = await resolver.ResolveAsync(cancellationToken);
            await tracker.CompleteAsync(
                run,
                created: 0,
                updated: result.Updated,
                failed: result.Failed + result.Deactivated,
                ct: cancellationToken);
            logger.LogInformation(
                "RSS feed resolve: {Checked} checked, {Updated} URLs updated, {Deactivated} deactivated, {Failed} failed.",
                result.Checked,
                result.Updated,
                result.Deactivated,
                result.Failed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RSS feed resolve job failed.");
            await tracker.FailAsync(run, 0, 0, ex, cancellationToken);
        }

        EnqueueFeedIngest();
    }

    private void EnqueueFeedIngest()
    {
        backgroundJobs.Enqueue<RssOpinionSyncJob>(job => job.SyncAsync(CancellationToken.None));
        backgroundJobs.Enqueue<MediaIngestJob>(job => job.IngestAsync(CancellationToken.None));
        backgroundJobs.Enqueue<NewsIngestJob>(job => job.IngestAsync(CancellationToken.None));
        logger.LogInformation(
            "Queued RSS ingest jobs after URL resolve: {Opinion}, {Media}, {News}.",
            RssOpinionSyncJob.JobId,
            MediaIngestJob.JobId,
            NewsIngestJob.JobId);
    }
}
