using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Leagues;

/// <summary>
/// Custom leagues are private to members — global and country leagues are open to everyone.
/// </summary>
public static class LeagueAccessGuard
{
    public static async Task<bool> IsMemberAsync(
        AppDbContext db,
        League league,
        IUserContext user,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return false;
        }

        return await db.LeagueMembers.AnyAsync(
            m => m.LeagueId == league.Id &&
                 (user.IsAuthenticated
                     ? m.UserId == user.UserId
                     : m.AnonymousUserId == user.AnonymousUserId),
            ct);
    }

    /// <returns>null when access is allowed; otherwise an IResult to return from the endpoint.</returns>
    public static async Task<IResult?> RequireCustomLeagueMemberAsync(
        AppDbContext db,
        League league,
        IUserContext user,
        CancellationToken ct)
    {
        if (league.Kind != LeagueKind.Custom)
        {
            return null;
        }

        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return Results.Json(
                new { error = "Sign in or continue as guest to view this league." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!await IsMemberAsync(db, league, user, ct))
        {
            return Results.Json(
                new { error = "This league is private to its members." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        return null;
    }
}
