using BanterApp.Api.Common;
using BanterApp.Api.Services;
using FluentValidation;

namespace BanterApp.Api.Features.Errors;

public sealed record ClientErrorReportRequest(
    string Message,
    string? Stack = null,
    string? Route = null,
    string? Component = null,
    string? UserAgent = null,
    Dictionary<string, string>? Metadata = null,
    string? RequestId = null);

public sealed class ClientErrorReportValidator : AbstractValidator<ClientErrorReportRequest>
{
    public ClientErrorReportValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Stack).MaximumLength(8000);
        RuleFor(x => x.Route).MaximumLength(512);
        RuleFor(x => x.Component).MaximumLength(256);
        RuleFor(x => x.UserAgent).MaximumLength(512);
        RuleFor(x => x.RequestId).MaximumLength(64);
    }
}

public static class ClientErrorEndpoints
{
    public static void MapClientErrorEndpoints(this WebApplication app)
    {
        app.MapPost("/api/errors/client", ReportClientError)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.ClientErrorReport)
            .WithValidation<ClientErrorReportRequest>()
            .WithTags("Errors");
    }

    private static async Task<IResult> ReportClientError(
        ClientErrorReportRequest request,
        HttpContext http,
        IUserContext user,
        IErrorTrackingService errorTracking,
        CancellationToken ct)
    {
        var requestId = ApiResults.GetRequestId(http) ?? request.RequestId;
        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["component"] = request.Component,
            ["user_agent"] = request.UserAgent
        };

        if (request.Metadata is not null)
        {
            foreach (var (key, value) in request.Metadata)
            {
                metadata[key] = value;
            }
        }

        await errorTracking.TrackAsync(new ErrorTrackRequest
        {
            Source = "frontend",
            ErrorCode = ErrorCodes.UnknownError,
            MessageSafe = request.Message,
            MessageInternal = request.Message,
            StackTrace = request.Stack,
            RequestId = requestId,
            Route = request.Route ?? http.Request.Path.Value,
            Method = http.Request.Method,
            StatusCode = StatusCodes.Status200OK,
            UserId = user.UserId,
            Provider = "app",
            Metadata = metadata,
            Severity = "error"
        }, ct);

        return Results.Ok(new { success = true, requestId });
    }
}
