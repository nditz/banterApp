using System.Net;
using System.Text.Json;
using BanterApp.Api.Services;

namespace BanterApp.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IApplicationErrorLogger errorLogger,
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
        catch (Exception ex)
        {
            await errorLogger.LogExceptionAsync(
                "api",
                ex,
                category: context.GetEndpoint()?.DisplayName,
                requestMethod: context.Request.Method,
                requestPath: context.Request.Path.Value,
                statusCode: (int)HttpStatusCode.InternalServerError,
                ct: context.RequestAborted);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                error = "An unexpected error occurred.",
                detail = environment.IsDevelopment() ? ex.Message : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload), context.RequestAborted);
        }
    }
}
