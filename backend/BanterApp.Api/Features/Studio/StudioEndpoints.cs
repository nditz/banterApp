using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Studio;

public static class StudioEndpoints
{
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

        // Fall back to empty comparison when the user has no picks yet
        if (myPreds.Count == 0)
        {
            return Results.Ok(new StudioComparisonResponse([], 0, null, null));
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
            .Where(pp => matchIds.Contains(pp.MatchId) && pp.Pundit.Kind == PunditKind.Source)
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
}
