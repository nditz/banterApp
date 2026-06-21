using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Leagues;
using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Leaderboards;

public static class LeaderboardEndpoints
{
    private const int TopCount = 10;

    public static IEndpointRouteBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leaderboards").WithTags("Leaderboards");

        group.MapGet("/global", GetGlobalLeaderboard);
        group.MapGet("/leagues", GetDefaultLeagueLeaderboard);
        group.MapGet("/leagues/{leagueId:guid}", GetLeagueLeaderboard);
        group.MapGet("/pundits", GetPunditLeaderboard);
        group.MapGet("/friends", GetFriendsLeaderboard);

        return app;
    }

    private static async Task<IResult> GetGlobalLeaderboard(
        AppDbContext db,
        IUserContext userContext,
        CancellationToken ct)
    {
        var userEntries = await db.Users
            .Select(u => new
            {
                Id = (Guid?)u.Id,
                u.DisplayName,
                TotalPoints = u.Predictions.Sum(p => p.PointsAwarded),
                PredictionsCount = u.Predictions.Count,
                IsAnonymous = false
            })
            .Where(x => x.PredictionsCount > 0)
            .ToListAsync(ct);

        var anonEntries = await db.AnonymousUsers
            .Select(a => new
            {
                Id = (Guid?)a.Id,
                DisplayName = string.Empty,
                TotalPoints = a.Predictions.Sum(p => p.PointsAwarded),
                PredictionsCount = a.Predictions.Count,
                IsAnonymous = true
            })
            .Where(x => x.PredictionsCount > 0)
            .ToListAsync(ct);

        var currentId = userContext.UserId ?? userContext.AnonymousUserId;

        var ranked = userEntries.Concat(anonEntries)
            .OrderByDescending(x => x.TotalPoints)
            .ThenBy(x => x.DisplayName)
            .Select((e, i) => new LeaderboardEntry(
                e.Id,
                e.Id == currentId
                    ? "You"
                    : (string.IsNullOrWhiteSpace(e.DisplayName)
                        ? $"Guest-{e.Id.ToString()![..4].ToUpperInvariant()}"
                        : e.DisplayName),
                e.TotalPoints,
                e.PredictionsCount,
                i + 1,
                e.Id == currentId))
            .ToList();

        if (ranked.Count == 0)
        {
            return Results.Ok(BuildMockView(seed: 7, totalPlayers: 1842, myRank: 137, myPoints: 86));
        }

        return Results.Ok(ToView(ranked));
    }

    private static async Task<IResult> GetLeagueLeaderboard(
        Guid leagueId,
        AppDbContext db,
        IUserContext userContext,
        TournamentBonusScoringService bonusScoring,
        CancellationToken ct)
    {
        var league = await db.Leagues.FindAsync([leagueId], ct);
        if (league is null)
        {
            return Results.NotFound();
        }

        var access = await LeagueAccessGuard.RequireCustomLeagueMemberAsync(db, league, userContext, ct);
        if (access is not null)
        {
            return access;
        }

        var currentId = userContext.UserId ?? userContext.AnonymousUserId;
        var standings = await Leagues.LeagueEndpoints.BuildStandingsAsync(db, league, bonusScoring, ct);

        var ranked = standings
            .Select((s, i) => new LeaderboardEntry(
                s.UserId,
                s.DisplayName,
                s.TotalPoints,
                s.PredictionsCount,
                i + 1,
                s.UserId == currentId))
            .ToList();

        var view = ToView(ranked);
        return Results.Ok(new
        {
            leagueId,
            leagueName = league.Name,
            top = view.Top,
            me = view.Me,
            totalPlayers = view.TotalPlayers
        });
    }

    private static async Task<IResult> GetPunditLeaderboard(AppDbContext db, CancellationToken ct)
    {
        var pundits = await db.Pundits
            .Where(p => p.Kind == PunditKind.Source)
            .Include(p => p.Predictions)
            .ThenInclude(pp => pp.Match)
            .Include(p => p.Opinions)
            .ToListAsync(ct);

        if (pundits.Count == 0)
        {
            return Results.Ok(Array.Empty<PunditLeaderboardEntry>());
        }

        var entries = pundits
            .Select(p =>
            {
                var finished = p.Predictions
                    .Where(pp => pp.Match is { Status: "FT" })
                    .ToList();
                var correct = finished.Count(pp =>
                {
                    var result = ScoringService.ResolveMatchResult(pp.Match!);
                    return pp.Prediction.Contains(result, StringComparison.OrdinalIgnoreCase) ||
                           (result == "H" && pp.Prediction.Contains("Home", StringComparison.OrdinalIgnoreCase)) ||
                           (result == "A" && pp.Prediction.Contains("Away", StringComparison.OrdinalIgnoreCase)) ||
                           (result == "D" && pp.Prediction.Contains("Draw", StringComparison.OrdinalIgnoreCase));
                });

                var display = PunditDisplayResolver.Resolve(p);
                var total = finished.Count > 0 ? finished.Count : p.Opinions.Count;
                var score = finished.Count > 0 ? correct : p.Opinions.Count;

                return new PunditLeaderboardEntry(
                    p.Id,
                    display.DisplayName,
                    display.DeskLabel,
                    display.Archetype,
                    display.ParodyCue,
                    display.StyleSlug,
                    display.IsFictionalPersona,
                    display.AttributionNote,
                    display.AvatarSeed,
                    display.SourceUrl,
                    score,
                    total,
                    0);
            })
            .OrderByDescending(e => e.TotalPredictions > 0 && e.CorrectPredictions <= e.TotalPredictions
                ? (double)e.CorrectPredictions / e.TotalPredictions
                : 0)
            .ThenByDescending(e => e.TotalPredictions)
            .ThenBy(e => e.Name)
            .Select((e, i) => e with { Rank = i + 1 })
            .Take(TopCount)
            .ToList();

        return Results.Ok(entries);
    }

    private static IResult GetDefaultLeagueLeaderboard() =>
        Results.Ok(BuildMockView(seed: 21, totalPlayers: 64, myRank: 14, myPoints: 142));

    private static IResult GetFriendsLeaderboard() =>
        Results.Ok(BuildMockView(seed: 42, totalPlayers: 18, myRank: 4, myPoints: 142));

    /// <summary>Top 10 + the current user's pinned row + total player count (FPL-style).</summary>
    private static LeaderboardView ToView(IReadOnlyList<LeaderboardEntry> ranked)
    {
        var top = ranked.Take(TopCount).ToList();
        var me = ranked.FirstOrDefault(e => e.IsCurrentUser);
        return new LeaderboardView(top, me, ranked.Count);
    }

    private static LeaderboardView BuildMockView(int seed, int totalPlayers, int myRank, int myPoints)
    {
        string[] names =
        [
            "WorldCupWizard", "PenaltyProphet", "GroupStageGuru", "GoldenBootGazer",
            "OffsideOracle", "HatTrickHero", "VARVeteran", "CornerKickKing",
            "NutmegNinja", "ExtraTimeExpert", "StoppageSage", "TopBinTactician"
        ];

        var random = new Random(seed);
        var basePoints = 150 + random.Next(40);

        var top = Enumerable.Range(0, Math.Min(TopCount, totalPlayers))
            .Select(i => new LeaderboardEntry(
                Guid.NewGuid(),
                names[i % names.Length],
                basePoints - i * (3 + random.Next(4)),
                12 + random.Next(8),
                i + 1))
            .ToList();

        LeaderboardEntry? me = null;
        if (myRank > 0 && myRank <= totalPlayers)
        {
            me = new LeaderboardEntry(null, "You", myPoints, 16, myRank, IsCurrentUser: true);
            if (myRank <= TopCount)
            {
                top[myRank - 1] = me;
            }
        }

        return new LeaderboardView(top, me, totalPlayers);
    }
}
