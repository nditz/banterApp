using BanterApp.Api.Common;
using BanterApp.Api.Services;

namespace BanterApp.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["X-XSS-Protection"] = "0";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

        if (environment.IsProduction())
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        await next(context);
    }
}
