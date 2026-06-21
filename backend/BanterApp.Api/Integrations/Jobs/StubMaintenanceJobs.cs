using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Pundits;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Jobs;

public sealed class StubMaintenanceJobs(
    AppDbContext db,
    SyncRunTracker runTracker,
    IRecurringJobManager recurringJobs,
    ILogger<StubMaintenanceJobs> logger)
{
    [AutomaticRetry(Attempts = 1)]
    public async Task RunAsync(string jobKey, CancellationToken ct)
    {
        if (string.Equals(jobKey, "failed-items.retry", StringComparison.OrdinalIgnoreCase))
        {
            await RetryFailedItemsAsync(ct);
            return;
        }

        if (string.Equals(jobKey, "stale-content.cleanup", StringComparison.OrdinalIgnoreCase))
        {
            await CleanupStaleContentAsync(ct);
        }
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task RetryFailedItemsAsync(CancellationToken ct)
    {
        var run = await runTracker.StartAsync("maintenance", "failed-items-retry", ct);
        var failedItems = await db.MediaItems
            .Where(i => i.ProcessingStatus == MediaItemProcessingStatus.Failed)
            .Take(100)
            .ToListAsync(ct);

        foreach (var item in failedItems)
        {
            item.ProcessingStatus = MediaItemProcessingStatus.Pending;
            item.ProcessingError = null;
        }

        await db.SaveChangesAsync(ct);

        if (failedItems.Count > 0)
        {
            recurringJobs.Trigger(ContentEnrichmentJob.JobId);
        }

        await runTracker.CompleteAsync(run, failedItems.Count, 0, 0, ct: ct);
        logger.LogInformation("Retried {Count} failed media items.", failedItems.Count);
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task CleanupStaleContentAsync(CancellationToken ct)
    {
        var run = await runTracker.StartAsync("maintenance", "stale-content-cleanup", ct);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var staleItems = await db.MediaItems
            .Where(i => i.ProcessingStatus == MediaItemProcessingStatus.Skipped && i.LastSyncedAt < cutoff)
            .Take(200)
            .ToListAsync(ct);

        db.MediaItems.RemoveRange(staleItems);
        await db.SaveChangesAsync(ct);
        await runTracker.CompleteAsync(run, staleItems.Count, 0, 0, ct: ct);
        logger.LogInformation("Removed {Count} stale skipped media items.", staleItems.Count);
    }
}
