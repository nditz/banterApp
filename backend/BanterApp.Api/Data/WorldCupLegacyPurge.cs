using BanterApp.Api.Features.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Data;

/// <summary>
/// Removes leftover non-Premier-League fixtures on boot (OpenFootball
/// <c>of26-*</c>, mis-stamped <c>apifb-*</c> rows). World Cup media, news,
/// RSS, and GIF rows are deleted by <c>scripts/purge-world-cup.sql</c> — they
/// are not scanned here so listing queries stay simple.
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
            db.PredictionReceipts.RemoveRange(
                await db.PredictionReceipts.Where(r => leftoverIds.Contains(r.MatchId)).ToListAsync(cancellationToken));
            db.BanterContentHistories.RemoveRange(
                await db.BanterContentHistories
                    .Where(h => h.MatchId != null && leftoverIds.Contains(h.MatchId))
                    .ToListAsync(cancellationToken));
            db.NewsFeedItems.RemoveRange(
                await db.NewsFeedItems
                    .Where(n => n.MatchId != null && leftoverIds.Contains(n.MatchId))
                    .ToListAsync(cancellationToken));
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

        if (leftoverIds.Count > 0 || leftoverPlayers.Count > 0)
        {
            logger?.LogWarning(
                "Non-PL fixture purge: {Matches} matches, {Players} national-squad players deactivated.",
                leftoverIds.Count,
                leftoverPlayers.Count);
        }

        return leftoverIds.Count;
    }
}
