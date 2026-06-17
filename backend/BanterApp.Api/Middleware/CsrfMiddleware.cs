using System.Security.Cryptography;

namespace BanterApp.Api.Middleware;

public sealed class CsrfMiddleware(RequestDelegate next)
{
    public const string CookieName = "banter_csrf";
    public const string HeaderName = "X-CSRF-Token";

    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS", "TRACE"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!SafeMethods.Contains(context.Request.Method))
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/sync/", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/api/auth/session/consent", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/api/auth/session/recover", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var cookieToken = context.Request.Cookies[CookieName];
            var headerToken = context.Request.Headers[HeaderName].ToString();

            if (string.IsNullOrWhiteSpace(cookieToken) ||
                string.IsNullOrWhiteSpace(headerToken) ||
                !string.Equals(cookieToken, headerToken, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "CSRF validation failed." });
                return;
            }
        }

        await next(context);
    }

    public static string IssueToken(HttpContext context)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            Secure = context.Request.IsHttps,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/"
        });
        return token;
    }
}
