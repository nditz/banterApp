using BanterApp.Api.Integrations.Common;
using Hangfire;

namespace BanterApp.Api.Integrations.Rss;

public sealed class RssFeedResolveJob(
    IRssFeedCatalog catalog,
    RssFeedResolver resolver,
    SyncRunTracker tracker,
    ILogger<RssFeedResolveJob> logger)
{
    public const string JobId = "rss-feed-resolve";
    private const string Provider = "rss-catalog";

    [AutomaticRetry(Attempts = 0)]
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
    }
}
