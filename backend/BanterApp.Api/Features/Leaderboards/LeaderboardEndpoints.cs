using BanterApp.Api.Data;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Leaderboards;

public static class LeaderboardEndpoints
{
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

    private static async Task<IResult> GetGlobalLeaderboard(AppDbContext db, CancellationToken ct)
    {
        var entries = await db.Users
            .Select(u => new
            {
                u.Id,
                u.DisplayName,
                TotalPoints = u.Predictions.Sum(p => p.PointsAwarded),
                PredictionsCount = u.Predictions.Count
            })
            .Where(x => x.PredictionsCount > 0)
            .OrderByDescending(x => x.TotalPoints)
            .ThenBy(x => x.DisplayName)
            .Take(100)
            .ToListAsync(ct);

        var ranked = entries
            .Select((e, i) => new LeaderboardEntry(e.Id, e.DisplayName, e.TotalPoints, e.PredictionsCount, i + 1))
            .ToList();

        if (ranked.Count == 0)
        {
            ranked = GetMockGlobalLeaderboard();
        }

        return Results.Ok(ranked);
    }

    private static async Task<IResult> GetLeagueLeaderboard(Guid leagueId, AppDbContext db, CancellationToken ct)
    {
        var league = await db.Leagues.FindAsync([leagueId], ct);
        if (league is null)
        {
            return Results.NotFound();
        }

        var memberIds = await db.LeagueMembers
            .Where(m => m.LeagueId == leagueId)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var entries = await db.Users
            .Where(u => memberIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.DisplayName,
                TotalPoints = u.Predictions.Sum(p => p.PointsAwarded),
                PredictionsCount = u.Predictions.Count
            })
            .OrderByDescending(x => x.TotalPoints)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(ct);

        var ranked = entries
            .Select((e, i) => new LeaderboardEntry(e.Id, e.DisplayName, e.TotalPoints, e.PredictionsCount, i + 1))
            .ToList();

        return Results.Ok(new { leagueId, leagueName = league.Name, standings = ranked });
    }

    private static async Task<IResult> GetPunditLeaderboard(AppDbContext db, ScoringService scoring, CancellationToken ct)
    {
        var pundits = await db.Pundits
            .Include(p => p.Predictions)
            .ThenInclude(pp => pp.Match)
            .ToListAsync(ct);

        if (pundits.Count == 0)
        {
            return Results.Ok(GetMockPunditLeaderboard());
        }

        var entries = pundits
            .Select(p =>
            {
                var finished = p.Predictions.Where(pp => pp.Match.Status == "FT").ToList();
                var correct = finished.Count(pp =>
                {
                    var result = ScoringService.ResolveMatchResult(pp.Match);
                    return pp.Prediction.Contains(result, StringComparison.OrdinalIgnoreCase) ||
                           (result == "H" && pp.Prediction.Contains("Home", StringComparison.OrdinalIgnoreCase)) ||
                           (result == "A" && pp.Prediction.Contains("Away", StringComparison.OrdinalIgnoreCase)) ||
                           (result == "D" && pp.Prediction.Contains("Draw", StringComparison.OrdinalIgnoreCase));
                });

                return new PunditLeaderboardEntry(p.Id, p.Name, p.Organization, correct, finished.Count, 0);
            })
            .OrderByDescending(e => e.CorrectPredictions)
            .ThenBy(e => e.Name)
            .Select((e, i) => e with { Rank = i + 1 })
            .ToList();

        return Results.Ok(entries);
    }

    private static IResult GetDefaultLeagueLeaderboard()
    {
        var ranked = new List<LeaderboardEntry>
        {
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "Alex", 156, 16, 1),
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), "You", 142, 16, 2),
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"), "Sam", 128, 16, 3),
        };

        return Results.Ok(ranked);
    }

    private static IResult GetFriendsLeaderboard()
    {
        var ranked = new List<LeaderboardEntry>
        {
            new(Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"), "Chris", 178, 16, 1),
            new(Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"), "You", 142, 16, 2),
            new(Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3"), "Taylor", 119, 16, 3),
        };

        return Results.Ok(ranked);
    }

    private static List<LeaderboardEntry> GetMockGlobalLeaderboard() =>
    [
        new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "WorldCupWizard", 42, 12, 1),
        new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "PenaltyProphet", 38, 11, 2),
        new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "GroupStageGuru", 35, 10, 3),
    ];

    private static List<PunditLeaderboardEntry> GetMockPunditLeaderboard() =>
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111101"), "Alex Morgan", "ESPN", 8, 12, 1),
        new(Guid.Parse("11111111-1111-1111-1111-111111111102"), "Rio Ferdinand", "BBC Sport", 7, 12, 2),
        new(Guid.Parse("11111111-1111-1111-1111-111111111103"), "Stephen A. Smith", "First Take", 5, 12, 3),
    ];
}
