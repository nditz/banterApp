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
        return TrackAsync(new ErrorTrackRequest
        {
            Source = source,
            ErrorCode = MapCategoryToCode(category, source),
            MessageSafe = message,
            MessageInternal = detail,
            Route = requestPath,
            Method = requestMethod,
            StatusCode = statusCode,
            JobKey = category,
            JobRunId = syncRunId,
            Provider = MapProvider(source, category)
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
            ErrorCode = MapCategoryToCode(category, source),
            MessageSafe = "An unexpected error occurred.",
            Route = requestPath,
            Method = requestMethod,
            StatusCode = statusCode ?? StatusCodes.Status500InternalServerError,
            JobKey = category,
            JobRunId = syncRunId,
            Provider = MapProvider(source, category)
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

    private static string MapCategoryToCode(string? category, string source)
    {
        if (string.Equals(source, "background", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(source, "job", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorCodes.JobFailed;
        }

        if (string.Equals(source, "frontend", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorCodes.UnknownError;
        }

        if (category?.Contains("openai", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ErrorCodes.OpenAiApiError;
        }

        if (category?.Contains("youtube", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ErrorCodes.YouTubeApiError;
        }

        if (category?.Contains("rss", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ErrorCodes.RssFetchError;
        }

        return ErrorCodes.InternalServerError;
    }

    private static string? MapProvider(string source, string? category)
    {
        if (category?.Contains("openai", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "openai";
        }

        if (category?.Contains("youtube", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "youtube";
        }

        if (category?.Contains("rss", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "rss";
        }

        return source switch
        {
            "api" or "backend" => "app",
            "background" or "job" => "job",
            "frontend" => "app",
            _ => "unknown"
        };
    }
}
