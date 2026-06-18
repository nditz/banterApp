using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Pundits;
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
        var rows = await db.PunditPredictions
            .Include(p => p.Pundit)
            .Include(p => p.Match)
            .Where(p => p.Match != null)
            .OrderByDescending(p => p.PublishedAt ?? p.Match!.KickoffTime)
            .Take(maxItems * 2)
            .ToListAsync(ct);

        var items = new List<FeedItemResponse>();

        foreach (var row in rows)
        {
            var match = row.Match!;
            if (!MatchOutcomeHelper.IsFinished(match))
            {
                continue;
            }

            var hit = MatchOutcomeHelper.PunditHit(row.Prediction, match);
            var display = row.Pundit is not null
                ? PunditDisplayResolver.Resolve(row.Pundit, row)
                : new PunditDisplay(
                    "Desk analyst",
                    "Pundit desk",
                    null,
                    "Parody · generic pundit desk",
                    null,
                    true,
                    row.SourceUrl,
                    PunditSourcePlatform.Normalize(row.SourceType),
                    PunditDisplayResolver.PersonaDisclaimer,
                    row.Id.ToString("N"));
            var scoreline = MatchOutcomeHelper.FormatScoreline(match);

            var media = FeedMediaMapper.FromGifMood(
                hit ? "celebrate" : "roast",
                hit ? "Pundit called it" : "Pundit roast");

            items.Add(new FeedItemResponse(
                $"pundit-{row.Id:N}",
                hit ? "prediction_highlight" : "banter",
                PunditDisplayResolver.FeedTitle(display, hit),
                PunditDisplayResolver.FeedBody(display, row.Prediction, scoreline, hit),
                media.Url,
                PunditDisplayResolver.FormatSourceLine(display),
                display.SourceUrl ?? row.SourceUrl,
                row.PublishedAt ?? match.KickoffTime,
                Random.Shared.Next(40, 400),
                Reactions: null,
                Media: media));

            if (items.Count >= maxItems)
            {
                break;
            }
        }

        return items;
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
            .Take(maxItems * 2)
            .ToListAsync(ct);

        var items = new List<FeedItemResponse>();

        foreach (var prediction in predictions)
        {
            var match = prediction.Match!;
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
                    hit ? "Ball knowledge" : "Receipts");

                items.Add(new FeedItemResponse(
                    $"you-post-{prediction.Id:N}",
                    hit ? "prediction_highlight" : "meme",
                    hit ? "Ball knowledge confirmed" : "That one aged like milk",
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
        var punditPick = await db.PunditPredictions
            .Include(p => p.Pundit)
            .Where(p => p.MatchId == matchId)
            .OrderByDescending(p => p.PublishedAt)
            .FirstOrDefaultAsync(ct);

        if (punditPick?.Pundit is null)
        {
            return string.Empty;
        }

        var display = PunditDisplayResolver.Resolve(punditPick.Pundit, punditPick);
        return $" {display.DisplayName} at {display.DeskLabel} had {punditPick.Prediction} on the desk.";
    }
}
