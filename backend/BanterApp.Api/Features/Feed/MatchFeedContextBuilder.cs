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
        IReadOnlyList<Guid>? followedPunditIds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            return null;
        }

        var opinionQuery = db.PunditOpinions
            .AsNoTracking()
            .Include(o => o.Pundit)
            .Include(o => o.SourceItem)
            .ThenInclude(i => i.MediaSource)
            .Where(o => o.MatchId == matchId &&
                        o.Pundit.Kind == PunditKind.Source &&
                        !o.NeedsHumanReview &&
                        o.ReviewStatus != "rejected");
        if (followedPunditIds is { Count: > 0 })
        {
            opinionQuery = opinionQuery.Where(o => followedPunditIds.Contains(o.PunditId));
        }

        var opinions = await opinionQuery
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

        var predictionQuery = db.PunditPredictions
            .AsNoTracking()
            .Include(p => p.Pundit)
            .Where(p => p.MatchId == matchId && p.Pundit.Kind == PunditKind.Source);
        if (followedPunditIds is { Count: > 0 })
        {
            predictionQuery = predictionQuery.Where(p => followedPunditIds.Contains(p.PunditId));
        }

        var predictions = await predictionQuery
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
