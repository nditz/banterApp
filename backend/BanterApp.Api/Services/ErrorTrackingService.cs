using System.Text.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Services;

public interface IErrorTrackingService
{
    Task<Guid> TrackAsync(ErrorTrackRequest request, CancellationToken ct = default);
    Task<Guid> TrackExceptionAsync(ErrorTrackRequest request, Exception exception, CancellationToken ct = default);
}

public sealed class ErrorTrackingService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<ErrorTrackingService> logger) : IErrorTrackingService
{
    private static readonly HashSet<string> ActiveStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "open", "investigating", "retry_scheduled"
    };

    public Task<Guid> TrackExceptionAsync(ErrorTrackRequest request, Exception exception, CancellationToken ct = default)
    {
        var enriched = new ErrorTrackRequest
        {
            Source = request.Source,
            ErrorCode = request.ErrorCode,
            MessageSafe = request.MessageSafe,
            Severity = request.Severity,
            ErrorType = request.ErrorType ?? exception.GetType().Name,
            MessageInternal = request.MessageInternal ?? exception.Message,
            StackTrace = request.StackTrace ?? exception.ToString(),
            RequestId = request.RequestId,
            Route = request.Route,
            Method = request.Method,
            StatusCode = request.StatusCode,
            UserId = request.UserId,
            AdminUserId = request.AdminUserId,
            JobKey = request.JobKey,
            JobRunId = request.JobRunId,
            SourceItemId = request.SourceItemId,
            Provider = request.Provider,
            ProviderRequestId = request.ProviderRequestId,
            Metadata = request.Metadata,
            IsRetryable = request.IsRetryable,
            RetryCount = request.RetryCount,
            NextRetryAt = request.NextRetryAt,
            SkipPersistence = request.SkipPersistence
        };

        return TrackAsync(enriched, ct);
    }

    public async Task<Guid> TrackAsync(ErrorTrackRequest request, CancellationToken ct = default)
    {
        var messageSafe = ErrorSanitizer.SanitizeMessage(request.MessageSafe) ?? string.Empty;
        var messageInternal = ErrorSanitizer.SanitizeMessage(request.MessageInternal ?? request.MessageSafe);
        var stackTrace = ErrorSanitizer.SanitizeStackTrace(request.StackTrace);
        var metadataJson = BuildMetadataJson(request);

        logger.Log(
            MapLogLevel(request.Severity),
            "Error tracked [{ErrorCode}] source={Source} route={Route} requestId={RequestId} provider={Provider} jobKey={JobKey}: {Message}",
            request.ErrorCode,
            request.Source,
            request.Route,
            request.RequestId,
            request.Provider,
            request.JobKey,
            messageSafe);

        if (request.SkipPersistence)
        {
            return Guid.Empty;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var fingerprint = ErrorFingerprinter.Compute(
                environment.EnvironmentName,
                request.Source,
                request.ErrorCode,
                request.ErrorType,
                request.Route,
                request.JobKey,
                request.Provider,
                messageSafe,
                stackTrace);

            var existing = await db.OperationalErrors
                .Where(e => e.Fingerprint == fingerprint && ActiveStatuses.Contains(e.Status))
                .OrderByDescending(e => e.LastSeenAt)
                .FirstOrDefaultAsync(ct);

            if (existing is not null)
            {
                existing.LastSeenAt = DateTimeOffset.UtcNow;
                existing.OccurrenceCount += 1;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                existing.RequestId = request.RequestId ?? existing.RequestId;
                existing.MetadataJson = MergeMetadata(existing.MetadataJson, metadataJson, request.RequestId);
                if (string.IsNullOrWhiteSpace(existing.StackTrace) && !string.IsNullOrWhiteSpace(stackTrace))
                {
                    existing.StackTrace = stackTrace;
                }

                await db.SaveChangesAsync(ct);
                return existing.Id;
            }

            var resolved = await db.OperationalErrors
                .Where(e => e.Fingerprint == fingerprint && (e.Status == "resolved" || e.Status == "ignored"))
                .OrderByDescending(e => e.LastSeenAt)
                .FirstOrDefaultAsync(ct);

            if (resolved is not null)
            {
                resolved.Status = "open";
                resolved.ResolvedAt = null;
                resolved.LastSeenAt = DateTimeOffset.UtcNow;
                resolved.OccurrenceCount += 1;
                resolved.UpdatedAt = DateTimeOffset.UtcNow;
                resolved.RequestId = request.RequestId ?? resolved.RequestId;
                resolved.MessageInternal = messageInternal;
                resolved.MetadataJson = MergeMetadata(resolved.MetadataJson, metadataJson, request.RequestId);
                if (!string.IsNullOrWhiteSpace(stackTrace))
                {
                    resolved.StackTrace = stackTrace;
                }

                await db.SaveChangesAsync(ct);
                return resolved.Id;
            }

            var row = new OperationalError
            {
                Id = Guid.NewGuid(),
                Fingerprint = fingerprint,
                RequestId = StringLimits.Truncate(request.RequestId, 64),
                Source = StringLimits.Truncate(request.Source, 32) ?? request.Source,
                Environment = StringLimits.Truncate(environment.EnvironmentName, 32) ?? environment.EnvironmentName,
                Severity = StringLimits.Truncate(request.Severity, 16) ?? "error",
                Status = "open",
                ErrorCode = StringLimits.Truncate(request.ErrorCode, 64) ?? request.ErrorCode,
                ErrorType = StringLimits.Truncate(request.ErrorType, 128),
                MessageSafe = StringLimits.Truncate(messageSafe, StringLimits.OperationalErrorMessage) ?? messageSafe,
                MessageInternal = StringLimits.Truncate(messageInternal, StringLimits.OperationalErrorInternal),
                StackTrace = StringLimits.Truncate(stackTrace, StringLimits.OperationalErrorStack),
                Route = StringLimits.Truncate(request.Route, 512),
                Method = StringLimits.Truncate(request.Method, 16),
                StatusCode = request.StatusCode,
                UserId = request.UserId,
                AdminUserId = request.AdminUserId,
                JobKey = StringLimits.Truncate(request.JobKey, 64),
                JobRunId = request.JobRunId,
                SourceItemId = request.SourceItemId,
                Provider = StringLimits.Truncate(request.Provider, 32),
                ProviderRequestId = StringLimits.Truncate(request.ProviderRequestId, 128),
                MetadataJson = metadataJson,
                FirstSeenAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
                OccurrenceCount = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.OperationalErrors.Add(row);
            await db.SaveChangesAsync(ct);
            return row.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist operational error for [{ErrorCode}]: {Message}", request.ErrorCode, messageSafe);
            return Guid.Empty;
        }
    }

    private static LogLevel MapLogLevel(string severity) =>
        severity.ToLowerInvariant() switch
        {
            "info" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "critical" => LogLevel.Critical,
            _ => LogLevel.Error
        };

    private static string? BuildMetadataJson(ErrorTrackRequest request)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (request.Metadata is not null)
        {
            foreach (var (key, value) in request.Metadata)
            {
                metadata[key] = value;
            }
        }

        if (request.IsRetryable)
        {
            metadata["is_retryable"] = true;
        }

        if (request.RetryCount > 0)
        {
            metadata["retry_count"] = request.RetryCount;
        }

        if (request.NextRetryAt.HasValue)
        {
            metadata["next_retry_at"] = request.NextRetryAt.Value;
        }

        if (metadata.Count == 0)
        {
            return null;
        }

        return ErrorSanitizer.SanitizeJson(JsonSerializer.Serialize(metadata));
    }

    private static string? MergeMetadata(string? existingJson, string? latestJson, string? requestId)
    {
        try
        {
            var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(existingJson))
            {
                var existing = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson);
                if (existing is not null)
                {
                    foreach (var (key, value) in existing)
                    {
                        merged[key] = value.ToString();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(latestJson))
            {
                var latest = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(latestJson);
                if (latest is not null)
                {
                    foreach (var (key, value) in latest)
                    {
                        merged[key] = value.ToString();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(requestId))
            {
                var recent = merged.TryGetValue("recent_request_ids", out var existingIds)
                    ? existingIds?.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? []
                    : [];

                recent.Insert(0, requestId);
                merged["recent_request_ids"] = string.Join(",", recent.Take(10));
            }

            return ErrorSanitizer.SanitizeJson(JsonSerializer.Serialize(merged));
        }
        catch
        {
            return latestJson ?? existingJson;
        }
    }
}
