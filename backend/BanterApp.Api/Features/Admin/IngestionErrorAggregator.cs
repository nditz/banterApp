using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Admin;

public sealed class IngestionErrorAggregator(AppDbContext db)
{
    public async Task SyncFromLogsAsync(CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var appErrors = await db.ApplicationErrorLogs
            .Where(e => e.OccurredAt >= since && e.Source == "background")
            .OrderByDescending(e => e.OccurredAt)
            .Take(200)
            .ToListAsync(ct);

        foreach (var error in appErrors)
        {
            await UpsertAsync(
                source: error.Source,
                jobKey: error.Category ?? "unknown",
                severity: "error",
                message: error.Message,
                stackTrace: error.Detail,
                syncRunId: error.SyncRunId,
                ct: ct);
        }

        var syncErrors = await db.SyncErrors
            .Where(e => e.OccurredAt >= since)
            .OrderByDescending(e => e.OccurredAt)
            .Take(200)
            .ToListAsync(ct);

        foreach (var error in syncErrors)
        {
            await UpsertAsync(
                source: error.Provider,
                jobKey: error.JobName,
                severity: "warning",
                message: error.Message,
                stackTrace: null,
                syncRunId: error.SyncRunId,
                ct: ct);
        }

        var failedItems = await db.MediaItems
            .Where(i => i.ProcessingStatus == MediaItemProcessingStatus.Failed && i.ProcessedAt >= since)
            .Take(100)
            .ToListAsync(ct);

        foreach (var item in failedItems)
        {
            await UpsertAsync(
                source: "media-item",
                jobKey: "ingestion",
                severity: "error",
                message: item.ProcessingError ?? "Media item processing failed.",
                stackTrace: null,
                mediaItemId: item.Id,
                ct: ct);
        }
    }

    private async Task UpsertAsync(
        string source,
        string jobKey,
        string severity,
        string message,
        string? stackTrace,
        Guid? syncRunId = null,
        Guid? mediaItemId = null,
        CancellationToken ct = default)
    {
        var normalizedMessage = message.Length > 500 ? message[..500] : message;
        var existing = await db.IngestionErrors.FirstOrDefaultAsync(
            e => e.Source == source && e.JobKey == jobKey && e.Message == normalizedMessage && e.Status != "resolved",
            ct);

        if (existing is null)
        {
            db.IngestionErrors.Add(new IngestionError
            {
                Id = Guid.NewGuid(),
                Source = source,
                JobKey = jobKey,
                Severity = severity,
                Message = normalizedMessage,
                StackTrace = stackTrace,
                Status = "open",
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
                Count = 1,
                SyncRunId = syncRunId,
                MediaItemId = mediaItemId
            });
        }
        else
        {
            existing.LastSeenAt = DateTimeOffset.UtcNow;
            existing.Count += 1;
            if (string.IsNullOrWhiteSpace(existing.StackTrace) && !string.IsNullOrWhiteSpace(stackTrace))
            {
                existing.StackTrace = stackTrace;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
