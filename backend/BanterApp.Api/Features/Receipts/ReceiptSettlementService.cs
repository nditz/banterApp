using System.Text.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Features.Studio;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Receipts;

public sealed class ReceiptSettlementService(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Idempotent emit: one receipt per prediction + result version. Does not SaveChanges.
    /// </summary>
    public async Task<int> EmitForPredictionsAsync(
        IReadOnlyList<Prediction> predictions,
        CancellationToken cancellationToken)
    {
        var finished = predictions
            .Where(p => p.Match is not null && MatchOutcomeHelper.IsFinished(p.Match) &&
                        p.Match.HomeScore.HasValue && p.Match.AwayScore.HasValue)
            .ToList();

        if (finished.Count == 0)
        {
            return 0;
        }

        var predictionIds = finished.Select(p => p.Id).ToList();
        var matchIds = finished.Select(p => p.MatchId).Distinct().ToList();

        var existing = await db.PredictionReceipts
            .Where(r => predictionIds.Contains(r.PredictionId))
            .Select(r => new { r.PredictionId, r.ResultHash })
            .ToListAsync(cancellationToken);
        var existingKeys = existing
            .Select(r => (r.PredictionId, r.ResultHash))
            .ToHashSet();

        var punditPreds = await StudioComparisonService.VisibleSourcePredictions(db)
            .Where(pp => matchIds.Contains(pp.MatchId))
            .Include(pp => pp.Pundit)
            .ToListAsync(cancellationToken);

        var resultPicks = await db.Predictions
            .AsNoTracking()
            .Where(p => matchIds.Contains(p.MatchId) && p.PredictionType == PredictionType.Result)
            .Select(p => new { p.MatchId, p.PointsAwarded })
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var prediction in finished)
        {
            var match = prediction.Match!;
            var hash = ReceiptStoryClassifier.ResultHash(match);
            if (!existingKeys.Add((prediction.Id, hash)))
            {
                continue;
            }

            var takes = punditPreds
                .Where(pp => pp.MatchId == prediction.MatchId)
                .Select(pp =>
                {
                    var display = PunditDisplayResolver.Resolve(pp.Pundit, pp);
                    return new ReceiptStoryClassifier.PunditTake(
                        pp.PunditId,
                        display.DisplayName,
                        pp.Prediction,
                        display.SourceUrl,
                        display.SourcePlatform,
                        MatchOutcomeHelper.PunditHit(pp.Prediction, match));
                })
                .ToList();

            var matchResultPicks = resultPicks.Where(p => p.MatchId == prediction.MatchId).ToList();
            var classification = ReceiptStoryClassifier.Classify(
                prediction,
                match,
                takes,
                matchResultPicks.Count,
                matchResultPicks.Count(p => p.PointsAwarded == 0));

            var now = DateTimeOffset.UtcNow;
            var receipt = new PredictionReceipt
            {
                Id = Guid.NewGuid(),
                PredictionId = prediction.Id,
                MatchId = prediction.MatchId,
                UserId = prediction.UserId,
                AnonymousUserId = prediction.AnonymousUserId,
                ResultHash = hash,
                PredictionType = prediction.PredictionType,
                PredictionValue = prediction.PredictionValue,
                PointsAwarded = prediction.PointsAwarded,
                AuraDelta = prediction.PointsAwarded,
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore,
                MatchStatus = match.Status,
                StoryType = classification.Primary,
                StoryTypesJson = JsonSerializer.Serialize(classification.All, JsonOptions),
                PunditTakesJson = JsonSerializer.Serialize(takes, JsonOptions),
                IsPublic = false,
                SettledAt = now,
                CreatedAt = now
            };

            foreach (var (type, summary) in classification.Candidates)
            {
                receipt.StoryCandidates.Add(new ReceiptStoryCandidate
                {
                    Id = Guid.NewGuid(),
                    StoryType = type,
                    Rank = receipt.StoryCandidates.Count,
                    Summary = summary
                });
            }

            db.PredictionReceipts.Add(receipt);
            created++;
        }

        return created;
    }
}
