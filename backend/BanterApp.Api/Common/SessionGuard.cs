using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Common;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Common;

public static class SessionGuard
{
    public static async Task<IResult?> RequireActiveSessionAsync(
        IUserContext user,
        HttpContext http,
        AppDbContext db,
        CancellationToken ct = default)
    {
        if (user.IsAuthenticated)
        {
            var registered = await db.Users.FindAsync([user.UserId!.Value], ct);
            if (registered?.TermsAcceptedAt is null)
            {
                return Results.Json(
                    new { error = "Accept terms to start predicting." },
                    statusCode: StatusCodes.Status403Forbidden);
            }

            return null;
        }

        if (!user.IsAnonymous)
        {
            return Results.Json(
                new { error = "Accept terms to start predicting." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var anonymous = http.Items["AnonymousUser"] as AnonymousUser;
        if (anonymous?.TermsAcceptedAt is null)
        {
            var loaded = await db.AnonymousUsers.FindAsync([user.AnonymousUserId!.Value], ct);
            if (loaded?.TermsAcceptedAt is null)
            {
                return Results.Json(
                    new { error = "Accept terms to start predicting." },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        }

        return null;
    }
}
