using System.Security.Cryptography;
using System.Text;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Common;

public sealed class SyncRunTracker(AppDbContext db)
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
        await db.SaveChangesAsync(ct);
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
        run.FinishedAt = DateTimeOffset.UtcNow;
        run.RecordsCreated = created;
        run.RecordsUpdated = updated;
        run.RecordsFailed = failed;
        run.Status = string.IsNullOrWhiteSpace(errorMessage) ? "completed" : "failed";
        run.ErrorMessage = errorMessage;
        await db.SaveChangesAsync(ct);
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
            EntityId = entityId,
            Message = message,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task UpsertExternalIdAsync(
        string entityType,
        string entityId,
        string provider,
        string providerExternalId,
        string? rawPayload = null,
        CancellationToken ct = default)
    {
        var hash = rawPayload is null ? null : ComputeHash(rawPayload);
        var existing = await db.ExternalIds.FirstOrDefaultAsync(
            x => x.Provider == provider &&
                 x.ProviderExternalId == providerExternalId &&
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
                ProviderExternalId = providerExternalId,
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

        await db.SaveChangesAsync(ct);
    }

    private static string ComputeHash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
