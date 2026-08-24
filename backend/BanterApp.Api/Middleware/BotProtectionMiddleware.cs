using BanterApp.Api.Common;
using BanterApp.Api.Services;

namespace BanterApp.Api.Middleware;

public sealed class BotProtectionMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    IApplicationErrorLogger errorLogger,
    IWebHostEnvironment environment)
{
    private static readonly string[] DefaultBlockedAgents =
    [
        "curl/",
        "wget/",
        "python-requests/",
        "scrapy/",
        "httpclient/",
        "go-http-client/",
        "java/",
        "libwww-perl/"
    ];

    private static readonly string[] DefaultSearchBots =
    [
        "googlebot",
        "bingbot",
        "duckduckbot",
        "slurp",
        "yandexbot"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!configuration.GetValue("Security:BotProtectionEnabled", true))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var userAgent = context.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            if (environment.IsProduction() && IsProtectedPath(path))
            {
                await BlockAsync(context, "empty_user_agent");
                return;
            }
        }
        else
        {
            var lower = userAgent.ToLowerInvariant();
            var allowSearchBots = configuration.GetValue("Security:AllowSearchBots", true);
            if (allowSearchBots && DefaultSearchBots.Any(bot => lower.Contains(bot, StringComparison.Ordinal)))
            {
                await next(context);
                return;
            }

            var blockedAgents = configuration.GetSection("Security:BlockedUserAgents").Get<string[]>()
                ?? DefaultBlockedAgents;

            if (blockedAgents.Any(agent => lower.Contains(agent, StringComparison.OrdinalIgnoreCase)))
            {
                await BlockAsync(context, "blocked_user_agent");
                return;
            }
        }

        await next(context);
    }

    private static bool IsProtectedPath(string path) =>
        path.StartsWith("/api/feed", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api/opinions", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/api/ai", StringComparison.OrdinalIgnoreCase);

    private async Task BlockAsync(HttpContext context, string reason)
    {
        await errorLogger.LogAsync(
            "bot",
            $"Blocked request ({reason}) {context.Request.Method} {context.Request.Path}",
            category: "bot_blocked",
            requestMethod: context.Request.Method,
            requestPath: context.Request.Path.Value,
            statusCode: StatusCodes.Status403Forbidden,
            ct: context.RequestAborted);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new ApiErrorEnvelope(false, new ApiErrorBody(
            ErrorCodes.Forbidden,
            "Request blocked.",
            ApiResults.GetRequestId(context))), context.RequestAborted);
    }
}
