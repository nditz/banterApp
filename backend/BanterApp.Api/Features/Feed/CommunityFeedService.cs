using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Feed;

/// <summary>
/// Public, anonymized crowd cards for the homepage timeline. Aggregates only:
/// no user ids, display names, or receipt ids ever leave this service.
/// </summary>
public static class CommunityFeedService
{
    /// <summary>Minimum picks on a fixture before a crowd card is publishable.</summary>
    public const int MinimumPicksForCrowdCard = 3;

    public static async Task<List<FeedItemResponse>> BuildAsync(
        AppDbContext db,
        int maxItems,
        CancellationToken ct = default)
    {
        if (maxItems <= 0)
        {
            return [];
        }

        var horizon = DateTimeOffset.UtcNow.AddDays(-10);

        var grouped = await db.Predictions
            .AsNoTracking()
            .Where(p => p.PredictionType == PredictionType.Result)
            .Where(p => p.Match != null && p.Match.KickoffTime >= horizon)
            .GroupBy(p => new { p.MatchId, p.PredictionValue })
            .Select(g => new { g.Key.MatchId, g.Key.PredictionValue, Count = g.Count() })
            .ToListAsync(ct);

        if (grouped.Count == 0)
        {
            return [];
        }

        var matchIds = grouped.Select(g => g.MatchId).Distinct().ToList();
        var matches = await db.Matches
            .AsNoTracking()
            .Where(m => matchIds.Contains(m.Id))
            .ToListAsync(ct);

        var byMatch = matches
            .Where(PremierLeagueMatchScope.IsPremierLeague)
            .ToDictionary(m => m.Id);

        var items = new List<FeedItemResponse>();

        foreach (var group in grouped.GroupBy(g => g.MatchId))
        {
            if (!byMatch.TryGetValue(group.Key, out var match))
            {
                continue;
            }

            var total = group.Sum(g => g.Count);
            if (total < MinimumPicksForCrowdCard)
            {
                continue;
            }

            var top = group.OrderByDescending(g => g.Count).First();
            var share = (int)Math.Round(top.Count * 100.0 / total);
            var crowdLabel = DescribeOutcome(top.PredictionValue, match);
            var finished = MatchOutcomeHelper.IsFinished(match);
            var seed = match.Id.GetHashCode();

            if (finished)
            {
                var outcome = MatchOutcomeHelper.ResolveOutcome(match);
                if (outcome is null)
                {
                    continue;
                }

                var crowdWasRight = string.Equals(outcome, top.PredictionValue, StringComparison.OrdinalIgnoreCase);
                var scoreline = MatchOutcomeHelper.FormatScoreline(match);
                var media = FeedMediaMapper.FromGifMood(crowdWasRight ? "celebrate" : "roast", "Crowd receipts", seed);

                items.Add(new FeedItemResponse(
                    $"crowd-post-{match.Id}",
                    "leaderboard",
                    crowdWasRight ? "The crowd called it" : "The crowd got cooked",
                    crowdWasRight
                        ? $"{share}% of Ball Takes picks backed {crowdLabel}. Final: {scoreline}."
                        : $"{share}% of Ball Takes picks backed {crowdLabel}. Final: {scoreline}. Majority, meet reality.",
                    media.Url,
                    "Ball Takes crowd",
                    null,
                    match.KickoffTime.AddHours(2),
                    null,
                    Media: media,
                    MatchId: match.Id));
            }
            else
            {
                var media = FeedMediaMapper.FromGifMood("debate", "Crowd split", seed);
                items.Add(new FeedItemResponse(
                    $"crowd-pre-{match.Id}",
                    "leaderboard",
                    "Where the crowd is leaning",
                    $"{share}% of Ball Takes picks are on {crowdLabel} for {match.TeamA} v {match.TeamB}. Fade them or follow them.",
                    media.Url,
                    "Ball Takes crowd",
                    null,
                    match.KickoffTime.AddDays(-1),
                    null,
                    Media: media,
                    MatchId: match.Id));
            }

            if (items.Count >= maxItems)
            {
                break;
            }
        }

        return items;
    }

    private static string DescribeOutcome(string predictionValue, Match match) =>
        predictionValue.Trim().ToLowerInvariant() switch
        {
            "home" => $"{match.TeamA} to win",
            "away" => $"{match.TeamB} to win",
            "draw" => "the draw",
            _ => predictionValue
        };
}
