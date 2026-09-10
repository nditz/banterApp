using BanterApp.Api.Common;
using BanterApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Aura;

/// <summary>
/// Aura is the user-facing name for the points the server already awards. There is no second
/// currency and no client-side total — every number here is derived from settled predictions.
/// </summary>
public sealed record AuraSummaryResponse(
    int Total,
    int WeeklyChange,
    int Streak,
    int SettledPicks,
    int CorrectPicks,
    int? Rank,
    int? TotalPlayers,
    int? Percentile);

public static class AuraEndpoints
{
    private const int WeeklyWindowDays = 7;

    public static IEndpointRouteBuilder MapAuraEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/aura").WithTags("Aura");

        group.MapGet("/me", GetMyAura);

        return app;
    }

    private static async Task<IResult> GetMyAura(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        var identityId = user.UserId ?? user.AnonymousUserId;
        if (identityId is null)
        {
            return Results.Ok(new AuraSummaryResponse(0, 0, 0, 0, 0, null, null, null));
        }

        var summary = await BuildAsync(db, user, ct);
        return Results.Ok(summary);
    }

    public static async Task<AuraSummaryResponse> BuildAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        var isUser = user.UserId.HasValue;

        var predictions = await (isUser
                ? db.Predictions.Where(p => p.UserId == user.UserId)
                : db.Predictions.Where(p => p.AnonymousUserId == user.AnonymousUserId))
            .AsNoTracking()
            .Where(p => p.Match != null)
            .Select(p => new
            {
                p.PointsAwarded,
                p.Match!.KickoffTime,
                p.Match.Status,
                p.Match.HomeScore,
                p.Match.AwayScore
            })
            .ToListAsync(ct);

        var bonusPoints = await (isUser
                ? db.MatchweekBonuses.Where(b => b.UserId == user.UserId)
                : db.MatchweekBonuses.Where(b => b.AnonymousUserId == user.AnonymousUserId))
            .AsNoTracking()
            .SumAsync(b => b.PointsAwarded, ct);

        var total = predictions.Sum(p => p.PointsAwarded) + bonusPoints;

        var cutoff = DateTimeOffset.UtcNow.AddDays(-WeeklyWindowDays);
        var weeklyChange = predictions
            .Where(p => p.KickoffTime >= cutoff)
            .Sum(p => p.PointsAwarded);

        var settled = predictions
            .Where(p => p.HomeScore.HasValue && p.AwayScore.HasValue)
            .OrderByDescending(p => p.KickoffTime)
            .ToList();

        var streak = 0;
        foreach (var pick in settled)
        {
            if (pick.PointsAwarded <= 0)
            {
                break;
            }

            streak++;
        }

        var (rank, totalPlayers) = await ResolveRankAsync(db, total, ct);
        int? percentile = rank.HasValue && totalPlayers is > 1
            ? (int)Math.Round(100.0 * (totalPlayers.Value - rank.Value) / (totalPlayers.Value - 1))
            : null;

        return new AuraSummaryResponse(
            total,
            weeklyChange,
            streak,
            settled.Count,
            settled.Count(p => p.PointsAwarded > 0),
            rank,
            totalPlayers,
            percentile);
    }

    /// <summary>Global standing among everyone who has made at least one pick.</summary>
    private static async Task<(int? Rank, int? TotalPlayers)> ResolveRankAsync(
        AppDbContext db,
        int total,
        CancellationToken ct)
    {
        var userTotals = await db.Users
            .AsNoTracking()
            .Where(u => u.Predictions.Any())
            .Select(u => u.Predictions.Sum(p => p.PointsAwarded))
            .ToListAsync(ct);

        var anonTotals = await db.AnonymousUsers
            .AsNoTracking()
            .Where(a => a.Predictions.Any())
            .Select(a => a.Predictions.Sum(p => p.PointsAwarded))
            .ToListAsync(ct);

        var all = userTotals.Concat(anonTotals).ToList();
        if (all.Count == 0)
        {
            return (null, null);
        }

        var ahead = all.Count(t => t > total);
        return (ahead + 1, all.Count);
    }
}
