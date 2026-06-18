using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Studio;

public static class StudioEndpoints
{
    private static readonly (PunditPersonaSeed Persona, Dictionary<string, string> Picks)[] MockPundits =
    [
        (PunditPersonas.Defaults[0], new()
        {
            ["m1"] = "HOME", ["m2"] = "HOME", ["m3"] = "AWAY", ["m4"] = "HOME",
        }),
        (PunditPersonas.Defaults[1], new()
        {
            ["m1"] = "DRAW", ["m2"] = "HOME", ["m3"] = "HOME", ["m4"] = "AWAY",
        }),
        (PunditPersonas.Defaults[2], new()
        {
            ["m1"] = "HOME", ["m2"] = "DRAW", ["m3"] = "DRAW", ["m4"] = "HOME",
        }),
        (PunditPersonas.Defaults[3], new()
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
        TournamentBonusScoringService bonusScoring,
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
                var league = await db.Leagues.FindAsync([firstLeagueId], ct);
                if (league is not null)
                {
                    var standings = await Leagues.LeagueEndpoints.BuildStandingsAsync(db, league, bonusScoring, ct);
                    var myEntry = standings.FirstOrDefault(s => s.UserId == myId);
                    myLeagueRank = myEntry is not null
                        ? standings.OrderByDescending(s => s.TotalPoints).ToList().FindIndex(s => s.UserId == myId) + 1
                        : null;
                }
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
                    picks.Add(ToPunditPick(
                        pp.Pundit,
                        FormatPrediction(pp.Prediction, "result"),
                        pp));
                }
            }
            else
            {
                foreach (var (persona, pundPicks) in MockPundits)
                {
                    if (pundPicks.TryGetValue(matchId, out var pick))
                    {
                        picks.Add(ToPunditPick(
                            PunditPersonas.ToEntity(persona),
                            FormatPrediction(pick, "result")));
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

    private static StudioPickEntry ToPunditPick(
        Pundit pundit,
        string formattedPrediction,
        PunditPrediction? prediction = null)
    {
        var display = PunditDisplayResolver.Resolve(pundit, prediction);
        return new StudioPickEntry(
            display.DisplayName,
            "pundit",
            display.DeskLabel,
            formattedPrediction,
            "result",
            null,
            display.Archetype,
            display.ParodyCue,
            display.StyleSlug,
            display.IsFictionalPersona,
            display.AttributionNote,
            display.SourceUrl,
            display.SourcePlatform,
            display.AvatarSeed);
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
        var gary = PunditPersonas.Defaults[0];
        var rio = PunditPersonas.Defaults[1];
        var stephen = PunditPersonas.Defaults[2];
        var henri = PunditPersonas.Defaults[3];

        var matches = new List<StudioMatchComparison>
        {
            BuildMockMatch("m1", "Brazil", "Argentina", 2,
            [
                MockMePick("Home Win", "result", 3),
                MockLeaguePick("Boss Wandi", "Away Win", "result", null),
                MockLeaguePick("GoalOracle", "Draw", "result", null),
                ToPunditPick(PunditPersonas.ToEntity(gary), "Home Win"),
                ToPunditPick(PunditPersonas.ToEntity(rio), "Draw"),
                ToPunditPick(PunditPersonas.ToEntity(henri), "Home Win"),
            ]),
            BuildMockMatch("m2", "France", "Germany", 5,
            [
                MockMePick("2-1", "correct_score", 7),
                MockLeaguePick("Boss Wandi", "Home Win", "result", 3),
                MockLeaguePick("GoalOracle", "Home Win", "result", 3),
                ToPunditPick(PunditPersonas.ToEntity(gary), "Home Win"),
                ToPunditPick(PunditPersonas.ToEntity(stephen), "Draw"),
                ToPunditPick(PunditPersonas.ToEntity(henri), "Home Win"),
            ]),
            BuildMockMatch("m3", "Spain", "Morocco", 3,
            [
                MockMePick("Home Win", "result", null),
                MockLeaguePick("Boss Wandi", "Home Win", "result", null),
                ToPunditPick(PunditPersonas.ToEntity(gary), "Away Win"),
                ToPunditPick(PunditPersonas.ToEntity(rio), "Home Win"),
            ]),
        };

        return new StudioComparisonResponse(matches, 10, 2, 3);
    }

    private static StudioMatchComparison BuildMockMatch(
        string id,
        string teamA,
        string teamB,
        int daysOut,
        IEnumerable<StudioPickEntry> picks)
    {
        var kickoff = DateTimeOffset.UtcNow.AddDays(daysOut);
        return new StudioMatchComparison(id, teamA, teamB, kickoff, null, null, picks.ToList());
    }

    private static StudioPickEntry MockLeaguePick(
        string name,
        string prediction,
        string type,
        int? points) =>
        new(name, "league", null, prediction, type, points);

    private static StudioPickEntry MockMePick(string prediction, string type, int? points) =>
        new("You", "me", null, prediction, type, points);
}
