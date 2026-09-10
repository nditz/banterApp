using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Data;

/// <summary>
/// Removes leftover World Cup 2026 and other non-Premier-League rows so the live
/// product is PL-only. Filtering queries is not enough — production still had
/// OpenFootball <c>of26-*</c> fixtures, mis-stamped <c>apifb-*</c> WC rows, and
/// World Cup media items that kept feeding AI prompts.
/// </summary>
public static class WorldCupLegacyPurge
{
    public static async Task<int> ExecuteAsync(
        AppDbContext db,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var leftover = await db.Matches
            .WhereNonPremierLeague()
            .ToListAsync(cancellationToken);
        var leftoverIds = leftover.Select(m => m.Id).ToList();

        if (leftoverIds.Count > 0)
        {
            db.Predictions.RemoveRange(
                await db.Predictions.Where(p => leftoverIds.Contains(p.MatchId)).ToListAsync(cancellationToken));
            db.PunditPredictions.RemoveRange(
                await db.PunditPredictions.Where(p => leftoverIds.Contains(p.MatchId)).ToListAsync(cancellationToken));
            db.MatchEvents.RemoveRange(
                await db.MatchEvents.Where(e => leftoverIds.Contains(e.MatchId)).ToListAsync(cancellationToken));
            db.LineupPlayers.RemoveRange(
                await db.LineupPlayers.Where(p => leftoverIds.Contains(p.MatchId)).ToListAsync(cancellationToken));
            db.PunditOpinions.RemoveRange(
                await db.PunditOpinions
                    .Where(o => o.MatchId != null && leftoverIds.Contains(o.MatchId))
                    .ToListAsync(cancellationToken));
            db.ExternalIds.RemoveRange(
                await db.ExternalIds.Where(e => leftoverIds.Contains(e.EntityId)).ToListAsync(cancellationToken));
            db.Matches.RemoveRange(leftover);
        }

        var wcMedia = await OffFocusMediaItems(db.MediaItems).ToListAsync(cancellationToken);
        var wcMediaIds = wcMedia.Select(i => i.Id).ToHashSet();
        if (wcMediaIds.Count > 0)
        {
            db.PunditOpinions.RemoveRange(
                await db.PunditOpinions
                    .Where(o => wcMediaIds.Contains(o.SourceItemId))
                    .ToListAsync(cancellationToken));
            db.MediaItems.RemoveRange(wcMedia);
        }

        var wcNews = await db.NewsFeedItems
            .Where(n =>
                (n.MatchId != null && leftoverIds.Contains(n.MatchId)) ||
                n.Title.ToLower().Contains("world cup") ||
                n.Title.ToLower().Contains("world-cup") ||
                n.Title.ToLower().Contains("worldcup") ||
                (n.Summary != null && (
                    n.Summary.ToLower().Contains("world cup") ||
                    n.Summary.ToLower().Contains("world-cup") ||
                    n.Summary.ToLower().Contains("worldcup"))) ||
                n.Url.ToLower().Contains("world-cup") ||
                n.Url.ToLower().Contains("worldcup") ||
                n.Url.ToLower().Contains("fifa.com"))
            .ToListAsync(cancellationToken);
        var wcNewsIds = wcNews.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (wcNewsIds.Count > 0)
        {
            var children = await db.NewsFeedItems
                .Where(n => n.ParentItemId != null &&
                            wcNewsIds.Contains(n.ParentItemId) &&
                            !wcNewsIds.Contains(n.Id))
                .ToListAsync(cancellationToken);
            db.NewsFeedItems.RemoveRange(children);
            db.NewsFeedItems.RemoveRange(wcNews);
        }

        db.StandingRows.RemoveRange(
            await db.StandingRows.Where(s => s.GroupKey != "PL").ToListAsync(cancellationToken));

        var leftoverPlayers = await db.Players
            .Where(p => p.IsActive && p.ClubName == null)
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var player in leftoverPlayers)
        {
            player.IsActive = false;
            player.UpdatedAt = now;
        }

        var wcGifQueries = await db.GifSearchQueries
            .Where(q => q.IsActive && (
                q.Phrase.ToLower().Contains("world cup") ||
                q.Phrase.ToLower().Contains("world-cup") ||
                q.Phrase.ToLower().Contains("worldcup")))
            .ToListAsync(cancellationToken);
        foreach (var query in wcGifQueries)
        {
            query.IsActive = false;
        }

        var wcSources = await db.MediaSources
            .Where(s => s.IsActive && (
                s.Name.ToLower().Contains("world cup") ||
                s.Name.ToLower().Contains("world-cup") ||
                (s.RssUrl != null && (
                    s.RssUrl.ToLower().Contains("world-cup") ||
                    s.RssUrl.ToLower().Contains("worldcup") ||
                    s.RssUrl.ToLower().Contains("fifa.com"))) ||
                (s.SiteUrl != null && (
                    s.SiteUrl.ToLower().Contains("world-cup") ||
                    s.SiteUrl.ToLower().Contains("worldcup") ||
                    s.SiteUrl.ToLower().Contains("fifa.com")))))
            .ToListAsync(cancellationToken);
        foreach (var source in wcSources)
        {
            source.IsActive = false;
            source.UpdatedAt = now;
        }

        var wcRssFeeds = await db.RssFeeds
            .Where(f => f.IsActive && (
                f.Name.ToLower().Contains("world cup") ||
                f.RssUrl.ToLower().Contains("world-cup") ||
                f.RssUrl.ToLower().Contains("worldcup") ||
                f.RssUrl.ToLower().Contains("fifa.com") ||
                (f.SiteUrl != null && (
                    f.SiteUrl.ToLower().Contains("world-cup") ||
                    f.SiteUrl.ToLower().Contains("worldcup") ||
                    f.SiteUrl.ToLower().Contains("fifa.com")))))
            .ToListAsync(cancellationToken);
        foreach (var feed in wcRssFeeds)
        {
            feed.IsActive = false;
            feed.UpdatedAt = now;
        }

        var wcGenerated = await db.GeneratedContents
            .Where(g =>
                g.Prompt.ToLower().Contains("world cup") ||
                g.Prompt.ToLower().Contains("world-cup") ||
                g.Prompt.ToLower().Contains("worldcup") ||
                g.Output.ToLower().Contains("world cup") ||
                g.Output.ToLower().Contains("world-cup") ||
                g.Output.ToLower().Contains("worldcup"))
            .ToListAsync(cancellationToken);
        db.GeneratedContents.RemoveRange(wcGenerated);

        await db.SaveChangesAsync(cancellationToken);

        if (leftoverIds.Count > 0 ||
            wcNewsIds.Count > 0 ||
            leftoverPlayers.Count > 0 ||
            wcMedia.Count > 0 ||
            wcGifQueries.Count > 0 ||
            wcSources.Count > 0 ||
            wcRssFeeds.Count > 0 ||
            wcGenerated.Count > 0)
        {
            logger?.LogWarning(
                "Non-PL / World Cup purge: {Matches} matches, {News} news items, {Media} media items, {GifQueries} GIF queries deactivated, {Sources} sources deactivated, {Rss} RSS feeds deactivated, {Generated} generated rows, {Players} national-squad players deactivated.",
                leftoverIds.Count,
                wcNewsIds.Count,
                wcMedia.Count,
                wcGifQueries.Count,
                wcSources.Count,
                wcRssFeeds.Count,
                wcGenerated.Count,
                leftoverPlayers.Count);
        }

        return leftoverIds.Count;
    }

    private static IQueryable<MediaItem> OffFocusMediaItems(IQueryable<MediaItem> items) =>
        items.Where(i =>
            i.Title.ToLower().Contains("world cup") ||
            i.Title.ToLower().Contains("world-cup") ||
            i.Title.ToLower().Contains("worldcup") ||
            i.SourceUrl.ToLower().Contains("world-cup") ||
            i.SourceUrl.ToLower().Contains("worldcup") ||
            i.SourceUrl.ToLower().Contains("fifa.com") ||
            (i.Description != null && (
                i.Description.ToLower().Contains("world cup") ||
                i.Description.ToLower().Contains("world-cup") ||
                i.Description.ToLower().Contains("worldcup") ||
                i.Description.ToLower().Contains("fifa.com"))));
}
