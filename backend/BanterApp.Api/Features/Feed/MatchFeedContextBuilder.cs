using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Feed;

public static class MatchFeedContextBuilder
{
    public static async Task<string?> BuildPunditContextAsync(
        AppDbContext db,
        string? matchId,
        int maxTakes = 2,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            return null;
        }

        var opinions = await db.PunditOpinions
            .AsNoTracking()
            .Include(o => o.Pundit)
            .Include(o => o.SourceItem)
            .ThenInclude(i => i.MediaSource)
            .Where(o => o.MatchId == matchId &&
                        o.Pundit.Kind == PunditKind.Source &&
                        !o.NeedsHumanReview &&
                        o.ReviewStatus != "rejected")
            .OrderByDescending(o => o.Confidence ?? 0)
            .ThenByDescending(o => o.CreatedAt)
            .Take(maxTakes)
            .ToListAsync(cancellationToken);

        if (opinions.Count > 0)
        {
            return string.Join(
                " ",
                opinions.Select(o =>
                {
                    var publication = o.SourceItem.Publication ?? o.SourceItem.MediaSource.Name;
                    var take = o.Prediction ?? o.Opinion;
                    return $"{o.Pundit.Name} ({publication}) said {take}.";
                }));
        }

        var predictions = await db.PunditPredictions
            .AsNoTracking()
            .Include(p => p.Pundit)
            .Where(p => p.MatchId == matchId && p.Pundit.Kind == PunditKind.Source)
            .OrderByDescending(p => p.Confidence ?? 0)
            .ThenByDescending(p => p.PublishedAt)
            .Take(maxTakes)
            .ToListAsync(cancellationToken);

        if (predictions.Count == 0)
        {
            return null;
        }

        return string.Join(
            " ",
            predictions.Select(p => $"{p.Pundit.Name} had {p.Prediction} on the desk."));
    }
}
