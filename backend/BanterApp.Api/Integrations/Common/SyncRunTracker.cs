using System.Security.Cryptography;
using System.Text;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Jobs;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Common;

public sealed class SyncRunTracker(
    AppDbContext db,
    IErrorTrackingService errorTracking,
    ILogger<SyncRunTracker> logger)
{
    public async Task<SyncRun> StartAsync(string provider, string jobName, CancellationToken ct = default)
    {
        var run = new SyncRun
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            JobName = jobName,
            StartedAt = DateTimeOffset.UtcNow,
            Status = "running"
        };
        db.SyncRuns.Add(run);
        await SaveChangesSafeAsync(ct);
        return run;
    }

    public async Task CompleteAsync(
        SyncRun run,
        int created,
        int updated,
        int failed = 0,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        await FinalizeRunAsync(run.Id, created, updated, failed, errorMessage, ct);
    }

    public async Task FailAsync(
        SyncRun run,
        int created,
        int updated,
        Exception exception,
        CancellationToken ct = default)
    {
        db.ChangeTracker.Clear();

        var jobKey = ResolveJobKey(run.JobName);
        await errorTracking.TrackExceptionAsync(new ErrorTrackRequest
        {
            Source = "job",
            ErrorCode = ErrorCodes.JobFailed,
            MessageSafe = "A background task failed.",
            Severity = "error",
            JobKey = jobKey,
            JobRunId = run.Id,
            Provider = run.Provider,
            IsRetryable = true,
            Metadata = new Dictionary<string, object?>
            {
                ["job_name"] = run.JobName,
                ["retry_count"] = 0,
                ["next_retry_at"] = DateTimeOffset.UtcNow.AddSeconds(ProviderErrorMapper.ComputeRetryDelaySeconds(0))
            }
        }, exception, ct);

        try
        {
            await LogErrorAsync(
                run.Provider,
                run.JobName,
                "job",
                exception.Message,
                run.Id,
                ct: ct);
        }
        catch (Exception logEx)
        {
            logger.LogWarning(logEx, "Failed to write sync error for job {JobName}.", run.JobName);
        }

        await FinalizeRunAsync(run.Id, created, updated, failed: 1, exception.Message, ct);
    }

    public async Task LogErrorAsync(
        string provider,
        string jobName,
        string entityType,
        string message,
        Guid? syncRunId = null,
        string? entityId = null,
        CancellationToken ct = default)
    {
        db.SyncErrors.Add(new SyncError
        {
            Id = Guid.NewGuid(),
            SyncRunId = syncRunId,
            Provider = provider,
            JobName = jobName,
            EntityType = entityType,
            EntityId = StringLimits.Truncate(entityId, 64),
            Message = StringLimits.Truncate(message, StringLimits.SyncErrorMessage) ?? string.Empty,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await SaveChangesSafeAsync(ct);

        var jobKey = ResolveJobKey(jobName);
        await errorTracking.TrackAsync(new ErrorTrackRequest
        {
            Source = "job",
            ErrorCode = ErrorCodes.JobFailed,
            MessageSafe = message,
            Severity = "warning",
            JobKey = jobKey,
            JobRunId = syncRunId,
            Provider = provider,
            Metadata = new Dictionary<string, object?>
            {
                ["entity_type"] = entityType,
                ["entity_id"] = entityId
            }
        }, ct);
    }

    public async Task UpsertExternalIdAsync(
        string entityType,
        string entityId,
        string provider,
        string providerExternalId,
        string? rawPayload = null,
        CancellationToken ct = default)
    {
        var normalizedExternalId = ExternalIdNormalizer.Normalize(providerExternalId);
        var hash = rawPayload is null ? null : ComputeHash(rawPayload);
        var existing = await db.ExternalIds.FirstOrDefaultAsync(
            x => x.Provider == provider &&
                 x.ProviderExternalId == normalizedExternalId &&
                 x.EntityType == entityType,
            ct);

        if (existing is null)
        {
            db.ExternalIds.Add(new ExternalId
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                Provider = provider,
                ProviderExternalId = normalizedExternalId,
                LastSeenAt = DateTimeOffset.UtcNow,
                RawPayloadHash = hash
            });
        }
        else
        {
            existing.EntityId = entityId;
            existing.LastSeenAt = DateTimeOffset.UtcNow;
            if (hash is not null)
            {
                existing.RawPayloadHash = hash;
            }
        }

        await SaveChangesSafeAsync(ct);
    }

    private static string ResolveJobKey(string jobName)
    {
        var byHangfire = JobRegistry.FindByHangfireId(jobName);
        if (byHangfire is not null)
        {
            return byHangfire.Key;
        }

        var byKey = JobRegistry.FindByKey(jobName);
        return byKey?.Key ?? jobName;
    }

    private async Task FinalizeRunAsync(
        Guid runId,
        int created,
        int updated,
        int failed,
        string? errorMessage,
        CancellationToken ct)
    {
        db.ChangeTracker.Clear();

        var run = await db.SyncRuns.FindAsync([runId], ct);
        if (run is null)
        {
            logger.LogWarning("Sync run {RunId} not found while finalizing.", runId);
            return;
        }

        run.FinishedAt = DateTimeOffset.UtcNow;
        run.RecordsCreated = created;
        run.RecordsUpdated = updated;
        run.RecordsFailed = failed;
        run.ItemsProcessed = created + updated + failed;
        run.DurationMs = (long)(run.FinishedAt.Value - run.StartedAt).TotalMilliseconds;
        run.Status = string.IsNullOrWhiteSpace(errorMessage) ? "completed" : "failed";
        run.ErrorMessage = StringLimits.Truncate(errorMessage, StringLimits.ErrorMessage);

        await SaveChangesSafeAsync(ct);
    }

    private async Task SaveChangesSafeAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database save failed in SyncRunTracker.");
            db.ChangeTracker.Clear();
            await errorTracking.TrackExceptionAsync(new ErrorTrackRequest
            {
                Source = "job",
                ErrorCode = ErrorCodes.DatabaseError,
                MessageSafe = "Database operation failed.",
                Severity = "critical",
                Provider = "database"
            }, ex, ct);
            throw;
        }
    }

    private static string ComputeHash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
