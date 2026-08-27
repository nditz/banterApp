using BanterApp.Api.Features.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Data;

/// <summary>
/// Removes leftover World Cup 2026 rows so the live product is Premier League only.
/// Filtering queries is not enough — production still had OpenFootball <c>of26-*</c> fixtures.
/// </summary>
public static class WorldCupLegacyPurge
{
    public static async Task<int> ExecuteAsync(
        AppDbContext db,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var leftover = await db.Matches
            .WhereWorldCupLegacy()
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

        var wcNews = await db.NewsFeedItems
            .Where(n =>
                (n.MatchId != null && leftoverIds.Contains(n.MatchId)) ||
                n.Title.ToLower().Contains("world cup") ||
                (n.Summary != null && n.Summary.ToLower().Contains("world cup")) ||
                n.Url.ToLower().Contains("world-cup") ||
                n.Url.ToLower().Contains("worldcup"))
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

        await db.SaveChangesAsync(cancellationToken);

        if (leftoverIds.Count > 0 || wcNewsIds.Count > 0 || leftoverPlayers.Count > 0)
        {
            logger?.LogWarning(
                "World Cup legacy purge: {Matches} matches, {News} news items, {Players} national-squad players deactivated.",
                leftoverIds.Count,
                wcNewsIds.Count,
                leftoverPlayers.Count);
        }

        return leftoverIds.Count;
    }
}
