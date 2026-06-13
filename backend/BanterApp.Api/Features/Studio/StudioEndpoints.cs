using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Studio;

public static class StudioEndpoints
{
    // Well-known mock pundits for when no real pundit data exists
    private static readonly (string Name, string Org, Dictionary<string, string> Picks)[] MockPundits =
    [
        ("Alex Morgan", "ESPN", new()
        {
            ["m1"] = "HOME", ["m2"] = "HOME", ["m3"] = "AWAY", ["m4"] = "HOME",
        }),
        ("Rio Ferdinand", "BBC Sport", new()
        {
            ["m1"] = "DRAW", ["m2"] = "HOME", ["m3"] = "HOME", ["m4"] = "AWAY",
        }),
        ("Stephen A. Smith", "First Take", new()
        {
            ["m1"] = "HOME", ["m2"] = "DRAW", ["m3"] = "DRAW", ["m4"] = "HOME",
        }),
        ("Thierry Henry", "CBS Sports", new()
        {
            ["m1"] = "AWAY", ["m2"] = "HOME", ["m3"] = "HOME", ["m4"] = "DRAW",
        }),
    ];

    public static IEndpointRouteBuilder MapStudioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/studio").WithTags("Studio");
        group.MapGet("/comparison", GetComparison);
        return app;
    }

    private static async Task<IResult> GetComparison(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        // 1. Fetch the current user's predictions with match data
        IQueryable<Prediction> myPredQuery = user.IsAuthenticated
            ? db.Predictions.Where(p => p.UserId == user.UserId)
            : user.IsAnonymous
                ? db.Predictions.Where(p => p.AnonymousUserId == user.AnonymousUserId)
                : Enumerable.Empty<Prediction>().AsQueryable();

        var myPreds = await myPredQuery
            .Include(p => p.Match)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        // Fall back to mock data when the user has no picks yet
        if (myPreds.Count == 0)
        {
            return Results.Ok(BuildMockComparison());
        }

        var matchIds = myPreds.Select(p => p.MatchId).Distinct().ToList();
        var myTotalPoints = myPreds.Sum(p => p.PointsAwarded);

        // 2. Fetch league mates' predictions for the same matches
        var myId = user.UserId ?? user.AnonymousUserId;
        var leagueMemberships = await db.LeagueMembers
            .Where(m => user.IsAuthenticated
                ? m.UserId == user.UserId
                : m.AnonymousUserId == user.AnonymousUserId)
            .Select(m => m.LeagueId)
            .ToListAsync(ct);

        var mateIds = new Dictionary<Guid, string>(); // id → displayName
        int leagueTotal = 0;
        int? myLeagueRank = null;

        if (leagueMemberships.Count > 0)
        {
            var firstLeagueId = leagueMemberships[0];
            var leagueMembers = await db.LeagueMembers
                .Where(m => m.LeagueId == firstLeagueId)
                .ToListAsync(ct);
            leagueTotal = leagueMembers.Count;

            foreach (var m in leagueMembers)
            {
                var mId = m.UserId ?? m.AnonymousUserId;
                if (mId is not null && mId != myId)
                    mateIds[mId.Value] = m.DisplayName;
            }

            // Rough rank: count how many members have more total points
            if (leagueTotal > 1)
            {
                var standings = await Leagues.LeagueEndpoints.BuildStandingsAsync(db, firstLeagueId, ct);
                var myEntry = standings.FirstOrDefault(s => s.UserId == myId);
                myLeagueRank = myEntry is not null
                    ? standings.OrderByDescending(s => s.TotalPoints).ToList().FindIndex(s => s.UserId == myId) + 1
                    : null;
            }
        }

        // Mate predictions for the same matches
        var mateUserIds = mateIds.Keys.ToList();
        var matePreds = mateUserIds.Count > 0
            ? await db.Predictions
                .Where(p => matchIds.Contains(p.MatchId) &&
                            (mateUserIds.Contains(p.UserId ?? Guid.Empty) ||
                             mateUserIds.Contains(p.AnonymousUserId ?? Guid.Empty)))
                .ToListAsync(ct)
            : [];

        // 3. Fetch pundit predictions for those matches
        var punditPreds = await db.PunditPredictions
            .Where(pp => matchIds.Contains(pp.MatchId))
            .Include(pp => pp.Pundit)
            .ToListAsync(ct);

        // 4. Build per-match comparison rows
        var myPredByMatch = myPreds
            .GroupBy(p => p.MatchId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var matePredByMatch = matePreds
            .GroupBy(p => p.MatchId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var punditPredByMatch = punditPreds
            .GroupBy(pp => pp.MatchId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var matches = new List<StudioMatchComparison>();

        foreach (var matchId in matchIds)
        {
            var firstPred = myPredByMatch[matchId].First();
            var match = firstPred.Match;
            if (match is null) continue;

            var picks = new List<StudioPickEntry>();

            // My picks (could be multiple types: result, score, double)
            foreach (var p in myPredByMatch[matchId])
            {
                var typeStr = p.PredictionType.ToString().ToLowerInvariant() switch
                {
                    "correctscore" => "correct_score",
                    "doublechance" => "double_chance",
                    _ => "result"
                };
                picks.Add(new StudioPickEntry(
                    "You", "me", null,
                    FormatPrediction(p.PredictionValue, typeStr),
                    typeStr,
                    p.PointsAwarded));
            }

            // League mates
            if (matePredByMatch.TryGetValue(matchId, out var mateBatch))
            {
                var byMate = mateBatch.GroupBy(p =>
                    mateIds.TryGetValue(p.UserId ?? p.AnonymousUserId ?? Guid.Empty, out var dn) ? dn : "Mate");

                foreach (var group in byMate)
                {
                    foreach (var p in group)
                    {
                        var typeStr = p.PredictionType.ToString().ToLowerInvariant() switch
                        {
                            "correctscore" => "correct_score",
                            "doublechance" => "double_chance",
                            _ => "result"
                        };
                        picks.Add(new StudioPickEntry(
                            group.Key, "league", null,
                            FormatPrediction(p.PredictionValue, typeStr),
                            typeStr,
                            p.PointsAwarded));
                    }
                }
            }

            // Pundits (real data)
            if (punditPredByMatch.TryGetValue(matchId, out var punditBatch))
            {
                foreach (var pp in punditBatch)
                {
                    picks.Add(new StudioPickEntry(
                        pp.Pundit.Name, "pundit", pp.Pundit.Organization,
                        FormatPrediction(pp.Prediction, "result"),
                        "result", null));
                }
            }
            else
            {
                // Inject mock pundit picks so the UI always has something to compare
                foreach (var (name, org, pundPicks) in MockPundits)
                {
                    if (pundPicks.TryGetValue(matchId, out var pick))
                    {
                        picks.Add(new StudioPickEntry(
                            name, "pundit", org,
                            FormatPrediction(pick, "result"),
                            "result", null));
                    }
                }
            }

            var actualResult = match.Status == "FT"
                ? ResolveActualResult(match)
                : null;

            matches.Add(new StudioMatchComparison(
                matchId, match.TeamA, match.TeamB,
                match.KickoffTime, match.Status, actualResult, picks));
        }

        return Results.Ok(new StudioComparisonResponse(
            matches.OrderBy(m => m.KickoffTime).ToList(),
            myTotalPoints,
            myLeagueRank,
            leagueTotal > 0 ? leagueTotal : null));

    }

    private static string FormatPrediction(string value, string type) => type switch
    {
        "result" => value.ToUpperInvariant() switch
        {
            "HOME" or "H" => "Home Win",
            "AWAY" or "A" => "Away Win",
            "DRAW" or "D" => "Draw",
            _ => value
        },
        "double_chance" => value switch
        {
            "home_or_draw" => "Home or Draw",
            "away_or_draw" => "Away or Draw",
            "home_or_away" => "Win (no draw)",
            _ => value
        },
        "correct_score" => value, // e.g. "2-1"
        _ => value
    };

    private static string ResolveActualResult(Match m) =>
        m.HomeScore is not null && m.AwayScore is not null
            ? $"{m.HomeScore}-{m.AwayScore}"
            : "FT";

    // ─── Mock fallback ────────────────────────────────────────────────────────

    private static StudioComparisonResponse BuildMockComparison()
    {
        var matches = new List<StudioMatchComparison>
        {
            BuildMockMatch("m1", "Brazil", "Argentina", 2,
            [
                ("You",             "me",     null,         "Home Win",  "result", 3),
                ("Boss Wandi",      "league", null,         "Away Win",  "result", null),
                ("GoalOracle",      "league", null,         "Draw",      "result", null),
                ("Alex Morgan",     "pundit", "ESPN",       "Home Win",  "result", null),
                ("Rio Ferdinand",   "pundit", "BBC Sport",  "Draw",      "result", null),
                ("Thierry Henry",   "pundit", "CBS Sports", "Home Win",  "result", null),
            ]),
            BuildMockMatch("m2", "France", "Germany", 5,
            [
                ("You",             "me",     null,         "2-1",      "correct_score", 7),
                ("Boss Wandi",      "league", null,         "Home Win", "result",        3),
                ("GoalOracle",      "league", null,         "Home Win", "result",        3),
                ("Alex Morgan",     "pundit", "ESPN",       "Home Win", "result",        null),
                ("Stephen A. Smith","pundit", "First Take", "Draw",     "result",        null),
                ("Thierry Henry",   "pundit", "CBS Sports", "Home Win", "result",        null),
            ]),
            BuildMockMatch("m3", "Spain", "Morocco", 3,
            [
                ("You",             "me",     null,         "Home Win",  "result", null),
                ("Boss Wandi",      "league", null,         "Home Win",  "result", null),
                ("Alex Morgan",     "pundit", "ESPN",       "Away Win",  "result", null),
                ("Rio Ferdinand",   "pundit", "BBC Sport",  "Home Win",  "result", null),
            ]),
        };

        return new StudioComparisonResponse(matches, 10, 2, 3);
    }

    private static StudioMatchComparison BuildMockMatch(
        string id, string teamA, string teamB, int daysOut,
        IEnumerable<(string Name, string Role, string? Org, string Pred, string Type, int? Pts)> picks)
    {
        var kickoff = DateTimeOffset.UtcNow.AddDays(daysOut);
        var entries = picks
            .Select(p => new StudioPickEntry(p.Name, p.Role, p.Org, p.Pred, p.Type, p.Pts))
            .ToList();
        return new StudioMatchComparison(id, teamA, teamB, kickoff, null, null, entries);
    }
}
