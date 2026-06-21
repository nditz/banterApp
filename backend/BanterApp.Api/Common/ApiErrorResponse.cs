using BanterApp.Api.Middleware;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BanterApp.Api.Common;

public sealed record ApiErrorBody(
    string Code,
    string Message,
    string? RequestId = null,
    IReadOnlyDictionary<string, string[]>? Details = null,
    string? Detail = null);

public sealed record ApiErrorEnvelope(bool Success, ApiErrorBody Error);

public static class ApiResults
{
    public static string? GetRequestId(HttpContext? context) =>
        context?.Items[RequestIdMiddleware.ItemKey]?.ToString();

    public static IResult Error(
        HttpContext context,
        string code,
        string message,
        int statusCode,
        IReadOnlyDictionary<string, string[]>? details = null,
        string? detail = null)
    {
        var envelope = new ApiErrorEnvelope(false, new ApiErrorBody(
            code,
            message,
            GetRequestId(context),
            details,
            detail));

        return Results.Json(envelope, statusCode: statusCode);
    }

    public static IResult FromAppException(HttpContext context, AppException ex, IHostEnvironment? environment = null)
    {
        var detail = environment?.IsDevelopment() == true ? ex.Message : null;
        return Error(context, ex.Code, ex.SafeMessage, ex.StatusCode, ex.Details, detail);
    }

    public static IResult ValidationError(
        HttpContext context,
        IReadOnlyDictionary<string, string[]> details)
    {
        return Error(
            context,
            ErrorCodes.ValidationError,
            "Please check the submitted fields.",
            StatusCodes.Status400BadRequest,
            details);
    }

    public static IResult NotFound(HttpContext context, string message = "The requested resource was not found.") =>
        Error(context, ErrorCodes.NotFound, message, StatusCodes.Status404NotFound);

    public static IResult Forbidden(HttpContext context, string message = "You do not have permission to perform this action.") =>
        Error(context, ErrorCodes.Forbidden, message, StatusCodes.Status403Forbidden);

    public static IResult Unauthorized(HttpContext context, string message = "Authentication is required.") =>
        Error(context, ErrorCodes.AuthenticationRequired, message, StatusCodes.Status401Unauthorized);

    public static IResult RateLimited(HttpContext context, string message, int? retryAfterSeconds = null)
    {
        var result = Error(context, ErrorCodes.RateLimited, message, StatusCodes.Status429TooManyRequests);
        if (retryAfterSeconds.HasValue && result is JsonHttpResult<ApiErrorEnvelope> json)
        {
            context.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString();
        }

        return result;
    }

    public static IResult InternalError(HttpContext context, IHostEnvironment environment, Exception? ex = null)
    {
        var detail = environment.IsDevelopment() && ex is not null ? ex.Message : null;
        return Error(
            context,
            ErrorCodes.InternalServerError,
            "Something went wrong. Please try again.",
            StatusCodes.Status500InternalServerError,
            detail: detail);
    }
}
