using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Middleware;

public sealed class AnonymousUserMiddleware(RequestDelegate next)
{
    public const string AnonymousIdHeader = "X-Anonymous-Id";
    public const string AnonymousCookieName = "banter_anonymous_id";
    public const string RecoveryCookieName = "banter_recovery_code";

    public async Task InvokeAsync(HttpContext context, AppDbContext db, IUserContext userContext)
    {
        if (userContext is UserContext mutable && !mutable.IsAuthenticated)
        {
            var cookieId = ResolveCookieId(context);
            AnonymousUser? anonymousUser;
            try
            {
                anonymousUser = await db.AnonymousUsers
                    .FirstOrDefaultAsync(a => a.CookieId == cookieId, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Client disconnected or app is shutting down — not an application error.
                return;
            }

            if (anonymousUser is not null)
            {
                mutable.AnonymousUserId = anonymousUser.Id;
                mutable.AnonymousCookieId = anonymousUser.CookieId;
                context.Items["AnonymousUser"] = anonymousUser;

                if (!context.Request.Cookies.ContainsKey(AnonymousCookieName))
                {
                    context.Response.Cookies.Append(AnonymousCookieName, cookieId, new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = context.Request.IsHttps,
                        MaxAge = TimeSpan.FromDays(365)
                    });
                }
            }
        }

        await next(context);
    }

    public static string ResolveCookieId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(AnonymousIdHeader, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString().Trim();
        }

        if (context.Request.Cookies.TryGetValue(AnonymousCookieName, out var cookieValue) &&
            !string.IsNullOrWhiteSpace(cookieValue))
        {
            return cookieValue;
        }

        return Guid.NewGuid().ToString("N");
    }
}
