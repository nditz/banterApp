using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Feed;

/// <summary>
/// Builds feed cards from pundit takes (guests) or the signed-in user's picks (registered).
/// </summary>
public static class PersonalizedFeedService
{
    public static async Task<(string Mode, List<FeedItemResponse> Items)> BuildAsync(
        AppDbContext db,
        IUserContext user,
        int maxItems,
        CancellationToken ct)
    {
        var hasAccountPredictions = user.IsAuthenticated &&
            await db.Predictions.AnyAsync(p => p.UserId == user.UserId, ct);

        if (hasAccountPredictions)
        {
            var personal = await BuildPersonalFeedAsync(db, user, maxItems, ct);
            return ("personal", personal);
        }

        var pundit = await BuildPunditFeedAsync(db, maxItems, ct);
        return ("pundit", pundit);
    }

    private static async Task<List<FeedItemResponse>> BuildPunditFeedAsync(
        AppDbContext db,
        int maxItems,
        CancellationToken ct)
    {
        var sourceOpinions = await LoadSourcePunditOpinionFeedAsync(db, maxItems, ct);
        return sourceOpinions;
    }

    private static async Task<List<FeedItemResponse>> LoadSourcePunditOpinionFeedAsync(
        AppDbContext db,
        int maxItems,
        CancellationToken ct)
    {
        var opinions = await db.PunditOpinions
            .AsNoTracking()
            .Include(o => o.Pundit)
            .Include(o => o.SourceItem)
            .ThenInclude(i => i.MediaSource)
            .Where(o => o.Pundit.Kind == PunditKind.Source && !o.NeedsHumanReview && o.ReviewStatus != "rejected")
            .OrderByDescending(o => o.SourceItem.PublishedAt ?? o.CreatedAt)
            .Take(maxItems)
            .ToListAsync(ct);

        return opinions
            .Select(o => PunditOpinionFeedMapper.ToFeedItem(o, o.Pundit, o.SourceItem))
            .ToList();
    }

    private static async Task<List<FeedItemResponse>> BuildPersonalFeedAsync(
        AppDbContext db,
        IUserContext user,
        int maxItems,
        CancellationToken ct)
    {
        var predictions = await db.Predictions
            .Include(p => p.Match)
            .Where(p => p.UserId == user.UserId && p.Match != null)
            .OrderByDescending(p => p.Match!.KickoffTime)
            .Take(maxItems * 4)
            .ToListAsync(ct);

        var items = new List<FeedItemResponse>();

        foreach (var prediction in predictions)
        {
            var match = prediction.Match!;
            if (!PremierLeagueMatchScope.IsPremierLeague(match))
            {
                continue;
            }

            var pickLabel = MatchOutcomeHelper.FormatUserPick(prediction, match);
            var finished = MatchOutcomeHelper.IsFinished(match);

            if (!finished)
            {
                var media = FeedMediaMapper.FromGifMood("hype", "Pick locked in");
                items.Add(new FeedItemResponse(
                    $"you-pre-{prediction.Id:N}",
                    "banter",
                    "Your pick is on the record",
                    $"Locked in {pickLabel} for {match.TeamA} v {match.TeamB}. We'll react the second full time hits.",
                    media.Url,
                    "Your card",
                    null,
                    prediction.CreatedAt,
                    null,
                    Media: media));
            }
            else
            {
                var scoreline = MatchOutcomeHelper.FormatScoreline(match);
                var hit = prediction.PointsAwarded > 0;
                var punditContrast = await GetPunditContrastAsync(db, match.Id, ct);

                var body = hit
                    ? $"You nailed {pickLabel}. Final: {scoreline}. +{prediction.PointsAwarded} pts in the bag.{punditContrast}"
                    : $"You called {pickLabel}. Final: {scoreline}. Receipts are public — run the burn script.{punditContrast}";

                var media = FeedMediaMapper.FromGifMood(
                    hit ? "celebrate" : "facepalm",
                    hit ? "Ball takes" : "Receipts");

                items.Add(new FeedItemResponse(
                    $"you-post-{prediction.Id:N}",
                    hit ? "prediction_highlight" : "meme",
                    hit ? "Ball takes confirmed" : "That one aged like milk",
                    body,
                    media.Url,
                    "Your picks",
                    null,
                    match.KickoffTime.AddHours(2),
                    hit ? prediction.PointsAwarded : null,
                    Media: media));
            }

            if (items.Count >= maxItems)
            {
                break;
            }
        }

        return items;
    }

    private static async Task<string> GetPunditContrastAsync(
        AppDbContext db,
        string matchId,
        CancellationToken ct)
    {
        var context = await MatchFeedContextBuilder.BuildPunditContextAsync(db, matchId, cancellationToken: ct);
        return string.IsNullOrWhiteSpace(context) ? string.Empty : $" {context}";
    }
}
