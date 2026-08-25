using BanterApp.Api.Data;
using BanterApp.Api.Integrations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Analytics;

/// <summary>
/// Deletes raw analytics events past the configured retention window. Runs daily and
/// reports through the standard sync-run history so it appears on /admin/jobs alongside
/// every other job.
/// </summary>
public sealed class AnalyticsRetentionJob(
    AppDbContext db,
    SyncRunTracker tracker,
    IOptions<AnalyticsOptions> options,
    ILogger<AnalyticsRetentionJob> logger)
{
    public const string JobId = "analytics-retention-cleanup";
    private const string Provider = "internal";

    public async Task CleanupAsync(CancellationToken ct = default)
    {
        var settings = options.Value;
        var retentionDays = Math.Max(settings.RawEventRetentionDays, 1);
        var batchSize = Math.Clamp(settings.RetentionDeleteBatchSize, 100, 50_000);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        var run = await tracker.StartAsync(Provider, JobId, ct);
        var deleted = 0;

        try
        {
            // Chunked so a large backlog cannot hold one long transaction open.
            while (!ct.IsCancellationRequested)
            {
                var batch = await db.AnalyticsEvents
                    .Where(e => e.OccurredAt < cutoff)
                    .OrderBy(e => e.OccurredAt)
                    .Take(batchSize)
                    .ToListAsync(ct);

                if (batch.Count == 0)
                {
                    break;
                }

                db.AnalyticsEvents.RemoveRange(batch);
                await db.SaveChangesAsync(ct);
                deleted += batch.Count;

                if (batch.Count < batchSize)
                {
                    break;
                }
            }

            logger.LogInformation(
                "Analytics retention removed {Deleted} events older than {Cutoff:u}.",
                deleted,
                cutoff);

            await tracker.CompleteAsync(run, created: 0, updated: deleted, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await tracker.FailAsync(run, created: 0, updated: deleted, ex, ct);
            throw;
        }
    }
}
