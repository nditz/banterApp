using System.Security.Claims;
using BanterApp.Api.Common;

namespace BanterApp.Api.Middleware;

/// <summary>
/// Populates <see cref="IUserContext"/> from validated Supabase JWT claims (sub = user id).
/// Works with Microsoft.AspNetCore.Authentication.JwtBearer configured in Program.cs.
/// </summary>
public sealed class SupabaseJwtMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        if (userContext is UserContext mutable &&
            context.User.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.User.FindFirstValue("sub");

            if (Guid.TryParse(sub, out var userId))
            {
                mutable.UserId = userId;
            }
        }

        await next(context);
    }
}
