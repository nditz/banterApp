using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Services;

public interface IApplicationErrorLogger
{
    Task LogAsync(
        string source,
        string message,
        string? category = null,
        string? detail = null,
        string? requestMethod = null,
        string? requestPath = null,
        int? statusCode = null,
        Guid? syncRunId = null,
        CancellationToken ct = default);

    Task LogExceptionAsync(
        string source,
        Exception exception,
        string? category = null,
        string? requestMethod = null,
        string? requestPath = null,
        int? statusCode = null,
        Guid? syncRunId = null,
        CancellationToken ct = default);
}

public sealed class ApplicationErrorLogger(
    IServiceScopeFactory scopeFactory,
    ILogger<ApplicationErrorLogger> logger) : IApplicationErrorLogger
{
    public Task LogAsync(
        string source,
        string message,
        string? category = null,
        string? detail = null,
        string? requestMethod = null,
        string? requestPath = null,
        int? statusCode = null,
        Guid? syncRunId = null,
        CancellationToken ct = default)
    {
        return PersistAsync(
            source,
            StringLimits.Truncate(message, StringLimits.ApplicationErrorMessage) ?? string.Empty,
            category,
            StringLimits.Truncate(detail, StringLimits.ApplicationErrorDetail),
            requestMethod,
            requestPath,
            statusCode,
            syncRunId,
            ct);
    }

    public Task LogExceptionAsync(
        string source,
        Exception exception,
        string? category = null,
        string? requestMethod = null,
        string? requestPath = null,
        int? statusCode = null,
        Guid? syncRunId = null,
        CancellationToken ct = default)
    {
        var detail = exception.ToString();
        return LogAsync(
            source,
            exception.Message,
            category,
            detail,
            requestMethod,
            requestPath,
            statusCode,
            syncRunId,
            ct);
    }

    private async Task PersistAsync(
        string source,
        string message,
        string? category,
        string? detail,
        string? requestMethod,
        string? requestPath,
        int? statusCode,
        Guid? syncRunId,
        CancellationToken ct)
    {
        logger.LogError(
            "Application error [{Source}/{Category}]: {Message}",
            source,
            category ?? "general",
            message);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ApplicationErrorLogs.Add(new ApplicationErrorLog
            {
                Id = Guid.NewGuid(),
                Source = StringLimits.Truncate(source, 32) ?? source,
                Category = StringLimits.Truncate(category, 128),
                Message = message,
                Detail = detail,
                RequestMethod = StringLimits.Truncate(requestMethod, 16),
                RequestPath = StringLimits.Truncate(requestPath, 512),
                StatusCode = statusCode,
                SyncRunId = syncRunId,
                OccurredAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to persist application error log for [{Source}/{Category}]: {Message}",
                source,
                category ?? "general",
                message);
        }
    }
}
