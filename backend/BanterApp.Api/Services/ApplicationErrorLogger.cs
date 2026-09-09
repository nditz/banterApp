using BanterApp.Api.Common;
using BanterApp.Api.Services;

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

public sealed class ApplicationErrorLogger(IServiceScopeFactory scopeFactory) : IApplicationErrorLogger
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
        var isBot = ErrorCategoryMapper.IsBot(source, category);
        return TrackAsync(new ErrorTrackRequest
        {
            Source = source,
            ErrorCode = ErrorCategoryMapper.Map(source, category),
            MessageSafe = message,
            MessageInternal = detail,
            Route = requestPath,
            Method = requestMethod,
            StatusCode = statusCode,
            JobKey = category,
            JobRunId = syncRunId,
            Provider = ErrorCategoryMapper.MapProvider(source, category),
            Severity = isBot ? "warning" : "error",
            SkipPersistence = isBot
        }, ct);
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
        return TrackExceptionAsync(new ErrorTrackRequest
        {
            Source = source,
            ErrorCode = ErrorCategoryMapper.Map(source, category),
            MessageSafe = "An unexpected error occurred.",
            Route = requestPath,
            Method = requestMethod,
            StatusCode = statusCode ?? StatusCodes.Status500InternalServerError,
            JobKey = category,
            JobRunId = syncRunId,
            Provider = ErrorCategoryMapper.MapProvider(source, category)
        }, exception, ct);
    }

    private async Task TrackAsync(ErrorTrackRequest request, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tracking = scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();
        await tracking.TrackAsync(request, ct);
    }

    private async Task TrackExceptionAsync(ErrorTrackRequest request, Exception exception, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tracking = scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();
        await tracking.TrackExceptionAsync(request, exception, ct);
    }
}
