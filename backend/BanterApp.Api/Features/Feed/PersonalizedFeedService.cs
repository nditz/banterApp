using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Feed;

/// <summary>
/// Builds feed cards from pundit takes (guests) or the viewer's own picks (signed-in or
/// anonymous session). Receipt rows are read owner-scoped to colour the copy, but receipt
/// ids are never emitted — receipts stay off the public timeline.
/// </summary>
public static class PersonalizedFeedService
{
    /// <summary>Share of a personalized page reserved for anonymized crowd cards.</summary>
    private const int CommunitySliceDivisor = 4;

    public static async Task<(string Mode, List<FeedItemResponse> Items)> BuildAsync(
        AppDbContext db,
        IUserContext user,
        int maxItems,
        IReadOnlyList<Guid>? followedPunditIds = null,
        CancellationToken ct = default)
    {
        var communityBudget = Math.Max(1, maxItems / CommunitySliceDivisor);
        var community = await CommunityFeedService.BuildAsync(db, communityBudget, ct);

        var hasOwnPredictions = (user.UserId.HasValue || user.AnonymousUserId.HasValue) &&
            await OwnedPredictions(db, user).AnyAsync(ct);

        if (hasOwnPredictions)
        {
            var personal = await BuildPersonalFeedAsync(
                db, user, Math.Max(1, maxItems - community.Count), followedPunditIds, ct);
            return ("personal", Interleave(personal, community, maxItems));
        }

        var pundit = await BuildPunditFeedAsync(
            db, Math.Max(1, maxItems - community.Count), followedPunditIds, ct);
        return ("pundit", Interleave(pundit, community, maxItems));
    }

    private static IQueryable<Prediction> OwnedPredictions(AppDbContext db, IUserContext user) =>
        user.UserId.HasValue
            ? db.Predictions.Where(p => p.UserId == user.UserId)
            : db.Predictions.Where(p => p.AnonymousUserId == user.AnonymousUserId);

    /// <summary>
    /// Weaves crowd cards through the primary cards so the timeline never renders as a
    /// single block of one card type.
    /// </summary>
    private static List<FeedItemResponse> Interleave(
        List<FeedItemResponse> primary,
        List<FeedItemResponse> community,
        int maxItems)
    {
        if (community.Count == 0)
        {
            return primary.Take(maxItems).ToList();
        }

        var merged = new List<FeedItemResponse>(maxItems);
        var communityIndex = 0;

        for (var i = 0; i < primary.Count && merged.Count < maxItems; i++)
        {
            merged.Add(primary[i]);

            var isSlot = (i + 1) % (CommunitySliceDivisor - 1) == 0;
            if (isSlot && communityIndex < community.Count && merged.Count < maxItems)
            {
                merged.Add(community[communityIndex++]);
            }
        }

        while (communityIndex < community.Count && merged.Count < maxItems)
        {
            merged.Add(community[communityIndex++]);
        }

        return merged;
    }

    private static async Task<List<FeedItemResponse>> BuildPunditFeedAsync(
        AppDbContext db,
        int maxItems,
        IReadOnlyList<Guid>? followedPunditIds,
        CancellationToken ct)
    {
        var sourceOpinions = await LoadSourcePunditOpinionFeedAsync(db, maxItems, followedPunditIds, ct);
        return sourceOpinions;
    }

    private static async Task<List<FeedItemResponse>> LoadSourcePunditOpinionFeedAsync(
        AppDbContext db,
        int maxItems,
        IReadOnlyList<Guid>? followedPunditIds,
        CancellationToken ct)
    {
        var query = db.PunditOpinions
            .AsNoTracking()
            .Include(o => o.Pundit)
            .Include(o => o.SourceItem)
            .ThenInclude(i => i.MediaSource)
            .Where(o => o.Pundit.Kind == PunditKind.Source && !o.NeedsHumanReview && o.ReviewStatus != "rejected");

        if (followedPunditIds is { Count: > 0 })
        {
            query = query.Where(o => followedPunditIds.Contains(o.PunditId));
        }

        var opinions = await query
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
        IReadOnlyList<Guid>? followedPunditIds,
        CancellationToken ct)
    {
        var predictions = await OwnedPredictions(db, user)
            .Include(p => p.Match)
            .Where(p => p.Match != null)
            .OrderByDescending(p => p.Match!.KickoffTime)
            .Take(maxItems * 4)
            .ToListAsync(ct);

        var items = new List<FeedItemResponse>();

        var unfinished = await BuildUnfinishedPicksCardAsync(db, user, ct);
        if (unfinished is not null)
        {
            items.Add(unfinished);
        }

        var settledStories = await LoadOwnedReceiptStoriesAsync(db, user, ct);

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
                    Media: media,
                    MatchId: match.Id));
            }
            else
            {
                var scoreline = MatchOutcomeHelper.FormatScoreline(match);
                var hit = prediction.PointsAwarded > 0;
                var punditContrast = await GetPunditContrastAsync(db, match.Id, followedPunditIds, ct);
                var settled = settledStories.TryGetValue(prediction.Id, out var storyType) ? storyType : null;

                var body = hit
                    ? $"You nailed {pickLabel}. Final: {scoreline}. +{prediction.PointsAwarded} pts in the bag.{punditContrast}"
                    : $"You called {pickLabel}. Final: {scoreline}. {DescribeReceiptPrompt(settled)}{punditContrast}";

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

    private static string DescribeReceiptPrompt(string? storyType) => storyType switch
    {
        "brave_but_wrong" => "Your private receipt says brave, but wrong.",
        "prediction_fraud" => "Your private receipt is filed under fraud.",
        null or "" => "Your private receipt is ready.",
        _ => "Your private receipt is ready."
    };

    /// <summary>
    /// Owner-scoped lookup used only to colour copy. Receipt ids are deliberately dropped;
    /// the dictionary is keyed by prediction id.
    /// </summary>
    private static async Task<Dictionary<Guid, string>> LoadOwnedReceiptStoriesAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        var query = user.UserId.HasValue
            ? db.PredictionReceipts.Where(r => r.UserId == user.UserId)
            : db.PredictionReceipts.Where(r => r.AnonymousUserId == user.AnonymousUserId);

        var rows = await query
            .AsNoTracking()
            .OrderByDescending(r => r.SettledAt)
            .Take(100)
            .Select(r => new { r.PredictionId, r.StoryType })
            .ToListAsync(ct);

        var map = new Dictionary<Guid, string>();
        foreach (var row in rows)
        {
            map[row.PredictionId] = row.StoryType;
        }

        return map;
    }

    /// <summary>
    /// Retention hook: nudges the viewer back to fixtures they have not picked yet.
    /// </summary>
    private static async Task<FeedItemResponse?> BuildUnfinishedPicksCardAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var upcoming = await db.Matches
            .AsNoTracking()
            .WherePremierLeague()
            .Where(m => m.KickoffTime > now && m.KickoffTime < now.AddDays(8))
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (upcoming.Count == 0)
        {
            return null;
        }

        var picked = await OwnedPredictions(db, user)
            .AsNoTracking()
            .Where(p => upcoming.Contains(p.MatchId))
            .Select(p => p.MatchId)
            .Distinct()
            .ToListAsync(ct);

        var remaining = upcoming.Count - picked.Count;
        if (remaining <= 0)
        {
            return null;
        }

        var media = FeedMediaMapper.FromGifMood("hype", "Picks still open", remaining);
        return new FeedItemResponse(
            "you-open-picks",
            "banter",
            remaining == 1 ? "One fixture still needs your call" : $"{remaining} fixtures still need your call",
            remaining == 1
                ? "There's a fixture kicking off this week with no take from you. No pick, no receipt."
                : $"{remaining} fixtures kick off this week and you haven't called them. No pick, no receipt.",
            media.Url,
            "Your card",
            null,
            now,
            null,
            Media: media);
    }

    private static async Task<string> GetPunditContrastAsync(
        AppDbContext db,
        string matchId,
        IReadOnlyList<Guid>? followedPunditIds,
        CancellationToken ct)
    {
        var context = await MatchFeedContextBuilder.BuildPunditContextAsync(
            db,
            matchId,
            followedPunditIds: followedPunditIds,
            cancellationToken: ct);
        return string.IsNullOrWhiteSpace(context) ? string.Empty : $" {context}";
    }
}
