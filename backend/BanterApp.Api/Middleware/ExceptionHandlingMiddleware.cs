using System.Net;
using BanterApp.Api.Common;
using BanterApp.Api.Middleware;
using BanterApp.Api.Services;

namespace BanterApp.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (AppException appEx)
        {
            await TrackAppExceptionAsync(context, appEx);
            if (!context.Response.HasStarted)
            {
                await WriteAppExceptionAsync(context, appEx, environment);
            }
            else
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            var requestId = context.Items[RequestIdMiddleware.ItemKey]?.ToString();
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                var errorTracking = scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();
                await errorTracking.TrackExceptionAsync(new ErrorTrackRequest
                {
                    Source = "backend",
                    ErrorCode = ErrorCodes.InternalServerError,
                    MessageSafe = "Something went wrong. Please try again.",
                    RequestId = requestId,
                    Route = context.Request.Path.Value,
                    Method = context.Request.Method,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Provider = "app"
                }, ex, context.RequestAborted);
            }

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new ApiErrorEnvelope(false, new ApiErrorBody(
                ErrorCodes.InternalServerError,
                "Something went wrong. Please try again.",
                requestId,
                Detail: environment.IsDevelopment() ? ex.Message : null));

            await context.Response.WriteAsJsonAsync(payload, context.RequestAborted);
        }
    }

    private async Task TrackAppExceptionAsync(HttpContext context, AppException appEx)
    {
        var severity = appEx switch
        {
            ValidationAppException => "info",
            RateLimitedAppException or ForbiddenAppException => "warning",
            _ => "error"
        };

        var request = new ErrorTrackRequest
        {
            Source = "backend",
            ErrorCode = appEx.Code,
            MessageSafe = appEx.SafeMessage,
            MessageInternal = appEx.Message,
            Severity = severity,
            RequestId = ApiResults.GetRequestId(context),
            Route = context.Request.Path.Value,
            Method = context.Request.Method,
            StatusCode = appEx.StatusCode,
            Provider = appEx is ProviderAppException providerEx ? providerEx.Provider : "app",
            IsRetryable = appEx.IsRetryable,
            SkipPersistence = appEx is ValidationAppException
        };

        await using var scope = scopeFactory.CreateAsyncScope();
        var errorTracking = scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();

        if (appEx is ProviderAppException provider)
        {
            await errorTracking.TrackAsync(new ErrorTrackRequest
            {
                Source = request.Source,
                ErrorCode = request.ErrorCode,
                MessageSafe = request.MessageSafe,
                MessageInternal = request.MessageInternal,
                Severity = request.Severity,
                ErrorType = request.ErrorType,
                StackTrace = request.StackTrace,
                RequestId = request.RequestId,
                Route = request.Route,
                Method = request.Method,
                StatusCode = request.StatusCode,
                UserId = request.UserId,
                AdminUserId = request.AdminUserId,
                JobKey = request.JobKey,
                JobRunId = request.JobRunId,
                SourceItemId = request.SourceItemId,
                Provider = provider.Provider,
                ProviderRequestId = provider.ProviderRequestId,
                Metadata = provider.Metadata,
                IsRetryable = request.IsRetryable,
                RetryCount = request.RetryCount,
                NextRetryAt = request.NextRetryAt,
                SkipPersistence = request.SkipPersistence
            }, context.RequestAborted);
            return;
        }

        await errorTracking.TrackAsync(request, context.RequestAborted);
    }

    private static Task WriteAppExceptionAsync(HttpContext context, AppException appEx, IHostEnvironment environment)
    {
        context.Response.Clear();
        context.Response.StatusCode = appEx.StatusCode;
        context.Response.ContentType = "application/json";

        if (appEx is RateLimitedAppException rateLimited && rateLimited.RetryAfterSeconds.HasValue)
        {
            context.Response.Headers.RetryAfter = rateLimited.RetryAfterSeconds.Value.ToString();
        }

        var envelope = new ApiErrorEnvelope(false, new ApiErrorBody(
            appEx.Code,
            appEx.SafeMessage,
            ApiResults.GetRequestId(context),
            appEx.Details,
            Detail: environment.IsDevelopment() ? appEx.Message : null));

        return context.Response.WriteAsJsonAsync(envelope);
    }
}
