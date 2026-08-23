using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Services;

public sealed class PredictionRescoreService(AppDbContext db, ScoringService scoring)
{
    public async Task<int> RescoreFinishedMatchesAsync(CancellationToken cancellationToken)
    {
        var finishedIds = await db.Matches
            .AsNoTracking()
            .Where(m => m.Status == "FT" && m.HomeScore != null && m.AwayScore != null)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (finishedIds.Count == 0)
        {
            return 0;
        }

        var predictions = await db.Predictions
            .Include(p => p.Match)
            .Where(p => finishedIds.Contains(p.MatchId))
            .ToListAsync(cancellationToken);

        var changed = 0;
        foreach (var prediction in predictions)
        {
            if (prediction.Match is null)
            {
                continue;
            }

            var points = scoring.CalculatePoints(
                prediction.PredictionType,
                prediction.PredictionValue,
                prediction.Match);
            if (prediction.PointsAwarded != points)
            {
                prediction.PointsAwarded = points;
                changed++;
            }
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }
}
