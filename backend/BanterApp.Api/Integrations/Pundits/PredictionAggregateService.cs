using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class PredictionAggregateService
{
    private readonly AppDbContext _db;

    public PredictionAggregateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> RefreshAsync(CancellationToken cancellationToken)
    {
        var opinions = await _db.PunditOpinions
            .AsNoTracking()
            .Where(o => !o.NeedsHumanReview && o.ReviewStatus != "rejected")
            .Where(o => !string.IsNullOrWhiteSpace(o.Team) || !string.IsNullOrWhiteSpace(o.Player))
            .ToListAsync(cancellationToken);

        var groups = opinions
            .SelectMany(o => BuildGroupKeys(o))
            .GroupBy(x => x.Key)
            .ToList();

        var updated = 0;
        foreach (var group in groups)
        {
            var parts = group.Key.Split('|');
            var entityType = parts[0];
            var entityName = parts[1];
            var predictionType = parts[2];
            var items = group.Select(x => x.Opinion).ToList();

            var positive = items.Count(o => IsPositive(o.Prediction));
            var negative = items.Count(o => IsNegative(o.Prediction));
            var neutral = items.Count - positive - negative;
            var avgConfidence = items.Where(o => o.Confidence.HasValue).Select(o => o.Confidence!.Value).DefaultIfEmpty(0.5).Average();

            var existing = await _db.PredictionAggregates.FirstOrDefaultAsync(
                a => a.EntityType == entityType &&
                     a.EntityName == entityName &&
                     a.PredictionType == predictionType,
                cancellationToken);

            if (existing is null)
            {
                _db.PredictionAggregates.Add(new PredictionAggregate
                {
                    Id = Guid.NewGuid(),
                    EntityType = entityType,
                    EntityName = entityName,
                    PredictionType = predictionType,
                    ConsensusSummary = BuildConsensusSummary(entityName, items),
                    PositiveCount = positive,
                    NegativeCount = negative,
                    NeutralCount = neutral,
                    SourceCount = items.Select(o => o.SourceItemId).Distinct().Count(),
                    ConfidenceScore = avgConfidence,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                existing.ConsensusSummary = BuildConsensusSummary(entityName, items);
                existing.PositiveCount = positive;
                existing.NegativeCount = negative;
                existing.NeutralCount = neutral;
                existing.SourceCount = items.Select(o => o.SourceItemId).Distinct().Count();
                existing.ConfidenceScore = avgConfidence;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }

            updated++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return updated;
    }

    private static IEnumerable<(string Key, PunditOpinion Opinion)> BuildGroupKeys(PunditOpinion opinion)
    {
        var predictionType = opinion.PredictionType ?? "unknown";

        if (!string.IsNullOrWhiteSpace(opinion.Team))
        {
            yield return ($"team|{opinion.Team.Trim()}|{predictionType}", opinion);
        }

        if (!string.IsNullOrWhiteSpace(opinion.Player))
        {
            yield return ($"player|{opinion.Player.Trim()}|{predictionType}", opinion);
        }

        if (!string.IsNullOrWhiteSpace(opinion.MatchId))
        {
            yield return ($"match|{opinion.MatchId}|{predictionType}", opinion);
        }
        else if (!string.IsNullOrWhiteSpace(opinion.MatchName))
        {
            yield return ($"match|{opinion.MatchName.Trim()}|{predictionType}", opinion);
        }

        if (!string.IsNullOrWhiteSpace(opinion.Topic) &&
            opinion.Topic.Contains("World Cup", StringComparison.OrdinalIgnoreCase))
        {
            yield return ($"tournament|{opinion.Topic.Trim()}|{predictionType}", opinion);
        }
    }

    private static bool IsPositive(string? prediction)
    {
        if (string.IsNullOrWhiteSpace(prediction))
        {
            return false;
        }

        return prediction.Contains("win", StringComparison.OrdinalIgnoreCase) ||
               prediction.Contains("favourite", StringComparison.OrdinalIgnoreCase) ||
               prediction.Contains("favorite", StringComparison.OrdinalIgnoreCase) ||
               prediction.Contains("dark horse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNegative(string? prediction)
    {
        if (string.IsNullOrWhiteSpace(prediction))
        {
            return false;
        }

        return prediction.Contains("lose", StringComparison.OrdinalIgnoreCase) ||
               prediction.Contains("exit", StringComparison.OrdinalIgnoreCase) ||
               prediction.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
               prediction.Contains("unlikely", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildConsensusSummary(string entityName, IReadOnlyList<PunditOpinion> items)
    {
        var predictions = items
            .Where(o => !string.IsNullOrWhiteSpace(o.Prediction))
            .Select(o => o.Prediction!)
            .Distinct()
            .Take(3)
            .ToList();

        if (predictions.Count == 0)
        {
            return $"{items.Count} pundit takes mention {entityName}.";
        }

        return $"{items.Count} sources: {string.Join("; ", predictions)}";
    }
}
