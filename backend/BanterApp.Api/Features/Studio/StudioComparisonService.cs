using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Features.Opinions;
using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Studio;

public sealed class StudioComparisonService(
    AppDbContext db,
    TournamentBonusScoringService bonusScoring,
    PunditFollowService follows)
{
    public async Task<StudioComparisonResponse> BuildAsync(
        IUserContext user,
        string? matchIdsCsv,
        CancellationToken ct)
    {
        var requestedIds = ParseMatchIds(matchIdsCsv);
        var matchweekMode = requestedIds.Count > 0;

        var followedIds = await follows.GetFollowedPunditIdsAsync(user, ct);
        var filteringToFollows = followedIds.Count > 0;

        IQueryable<Prediction> myPredQuery = user.IsAuthenticated
            ? db.Predictions.Where(p => p.UserId == user.UserId)
            : user.IsAnonymous
                ? db.Predictions.Where(p => p.AnonymousUserId == user.AnonymousUserId)
                : Enumerable.Empty<Prediction>().AsQueryable();

        if (matchweekMode)
        {
            myPredQuery = myPredQuery.Where(p => requestedIds.Contains(p.MatchId));
        }

        var myPreds = await myPredQuery
            .Include(p => p.Match)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        if (!matchweekMode && myPreds.Count == 0)
        {
            return new StudioComparisonResponse(
                [],
                0,
                null,
                null,
                followedIds.Count,
                filteringToFollows);
        }

        List<string> matchIds;
        Dictionary<string, Match> matchesById;
        if (matchweekMode)
        {
            var loaded = await db.Matches
                .Where(m => requestedIds.Contains(m.Id))
                .ToListAsync(ct);
            matchesById = loaded.ToDictionary(m => m.Id);
            matchIds = requestedIds.Where(matchesById.ContainsKey).ToList();
        }
        else
        {
            matchIds = myPreds.Select(p => p.MatchId).Distinct().ToList();
            matchesById = myPreds
                .Where(p => p.Match is not null)
                .GroupBy(p => p.MatchId)
                .ToDictionary(g => g.Key, g => g.First().Match!);
        }

        var myTotalPoints = myPreds.Sum(p => p.PointsAwarded);

        int leagueTotal = 0;
        int? myLeagueRank = null;
        var mateIds = new Dictionary<Guid, string>();
        List<Prediction> matePreds = [];

        if (!matchweekMode)
        {
            (mateIds, leagueTotal, myLeagueRank, matePreds) =
                await LoadLeagueMatesAsync(user, matchIds, ct);
        }

        var punditQuery = VisibleSourcePredictions(db)
            .Where(pp => matchIds.Contains(pp.MatchId));
        if (filteringToFollows)
        {
            punditQuery = punditQuery.Where(pp => followedIds.Contains(pp.PunditId));
        }

        var punditPreds = await punditQuery
            .Include(pp => pp.Pundit)
            .ToListAsync(ct);

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
            if (!matchesById.TryGetValue(matchId, out var match))
            {
                continue;
            }

            var picks = new List<StudioPickEntry>();
            var finished = MatchOutcomeHelper.IsFinished(match);

            if (myPredByMatch.TryGetValue(matchId, out var myBatch))
            {
                foreach (var p in myBatch)
                {
                    var typeStr = ToTypeString(p.PredictionType);
                    picks.Add(new StudioPickEntry(
                        "You",
                        "me",
                        null,
                        FormatPrediction(p.PredictionValue, typeStr),
                        typeStr,
                        p.PointsAwarded,
                        WasCorrect: finished ? p.PointsAwarded > 0 : null));
                }
            }

            if (matePredByMatch.TryGetValue(matchId, out var mateBatch))
            {
                var byMate = mateBatch.GroupBy(p =>
                    mateIds.TryGetValue(p.UserId ?? p.AnonymousUserId ?? Guid.Empty, out var dn)
                        ? dn
                        : "Mate");

                foreach (var group in byMate)
                {
                    foreach (var p in group)
                    {
                        var typeStr = ToTypeString(p.PredictionType);
                        picks.Add(new StudioPickEntry(
                            group.Key,
                            "league",
                            null,
                            FormatPrediction(p.PredictionValue, typeStr),
                            typeStr,
                            p.PointsAwarded,
                            WasCorrect: finished ? p.PointsAwarded > 0 : null));
                    }
                }
            }

            if (punditPredByMatch.TryGetValue(matchId, out var punditBatch))
            {
                foreach (var pp in punditBatch)
                {
                    picks.Add(ToPunditPick(pp, match, finished));
                }
            }

            var actualResult = finished ? ResolveActualResult(match) : null;

            matches.Add(new StudioMatchComparison(
                matchId,
                match.TeamA,
                match.TeamB,
                match.KickoffTime,
                match.Status,
                actualResult,
                picks));
        }

        IReadOnlyList<StudioMatchComparison> ordered = matchweekMode
            ? matches
            : matches.OrderBy(m => m.KickoffTime).ToList();

        return new StudioComparisonResponse(
            ordered,
            myTotalPoints,
            myLeagueRank,
            leagueTotal > 0 ? leagueTotal : null,
            followedIds.Count,
            filteringToFollows);
    }

    internal static IQueryable<PunditPrediction> VisibleSourcePredictions(AppDbContext db) =>
        db.PunditPredictions.Where(pp =>
            pp.Pundit.Kind == PunditKind.Source &&
            (!db.PunditOpinions.Any(o => o.PunditId == pp.PunditId && o.MatchId == pp.MatchId) ||
             db.PunditOpinions.Any(o =>
                 o.PunditId == pp.PunditId &&
                 o.MatchId == pp.MatchId &&
                 !o.NeedsHumanReview &&
                 o.ReviewStatus != "rejected")));

    private async Task<(
            Dictionary<Guid, string> MateIds,
            int LeagueTotal,
            int? MyLeagueRank,
            List<Prediction> MatePreds)>
        LoadLeagueMatesAsync(IUserContext user, List<string> matchIds, CancellationToken ct)
    {
        var myId = user.UserId ?? user.AnonymousUserId;
        var mateIds = new Dictionary<Guid, string>();
        int leagueTotal = 0;
        int? myLeagueRank = null;

        var leagueMemberships = await db.LeagueMembers
            .Where(m => user.IsAuthenticated
                ? m.UserId == user.UserId
                : m.AnonymousUserId == user.AnonymousUserId)
            .Select(m => m.LeagueId)
            .ToListAsync(ct);

        if (leagueMemberships.Count == 0)
        {
            return (mateIds, 0, null, []);
        }

        var firstLeagueId = leagueMemberships[0];
        var leagueMembers = await db.LeagueMembers
            .Where(m => m.LeagueId == firstLeagueId)
            .ToListAsync(ct);
        leagueTotal = leagueMembers.Count;

        foreach (var m in leagueMembers)
        {
            var mId = m.UserId ?? m.AnonymousUserId;
            if (mId is not null && mId != myId)
            {
                mateIds[mId.Value] = m.DisplayName;
            }
        }

        if (leagueTotal > 1)
        {
            var league = await db.Leagues.FindAsync([firstLeagueId], ct);
            if (league is not null)
            {
                var standings = await Leagues.LeagueEndpoints.BuildStandingsAsync(db, league, bonusScoring, ct);
                var ranked = standings.OrderByDescending(s => s.TotalPoints).ToList();
                var idx = ranked.FindIndex(s => s.UserId == myId);
                myLeagueRank = idx >= 0 ? idx + 1 : null;
            }
        }

        var mateUserIds = mateIds.Keys.ToList();
        var matePreds = mateUserIds.Count > 0
            ? await db.Predictions
                .Where(p => matchIds.Contains(p.MatchId) &&
                            (mateUserIds.Contains(p.UserId ?? Guid.Empty) ||
                             mateUserIds.Contains(p.AnonymousUserId ?? Guid.Empty)))
                .ToListAsync(ct)
            : [];

        return (mateIds, leagueTotal, myLeagueRank, matePreds);
    }

    private static StudioPickEntry ToPunditPick(PunditPrediction pp, Match match, bool finished)
    {
        var display = PunditDisplayResolver.Resolve(pp.Pundit, pp);
        bool? wasCorrect = finished ? MatchOutcomeHelper.PunditHit(pp.Prediction, match) : null;
        return new StudioPickEntry(
            display.DisplayName,
            "pundit",
            display.DeskLabel,
            FormatPrediction(pp.Prediction, "result"),
            "result",
            null,
            display.Archetype,
            display.ParodyCue,
            display.StyleSlug,
            display.IsFictionalPersona,
            display.AttributionNote,
            display.SourceUrl,
            display.SourcePlatform,
            display.AvatarSeed,
            wasCorrect);
    }

    private static string ToTypeString(PredictionType type) =>
        type.ToString().ToLowerInvariant() switch
        {
            "correctscore" => "correct_score",
            "doublechance" => "double_chance",
            _ => "result"
        };

    public static string FormatPrediction(string value, string type) => type switch
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
        "correct_score" => value,
        _ => value
    };

    private static string? ResolveActualResult(Match m) =>
        m.HomeScore is not null && m.AwayScore is not null
            ? $"{m.HomeScore}-{m.AwayScore}"
            : "FT";

    private static List<string> ParseMatchIds(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .Take(40)
            .ToList();
    }
}
