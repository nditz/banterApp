using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Pundits;

/// <summary>
/// Writes match-linked <see cref="PunditPrediction"/> rows from reviewed opinions.
/// Extract jobs already call this path via persistence; admin approve uses it as a backfill.
/// </summary>
public sealed class PunditMatchPredictionSync(AppDbContext db)
{
    public async Task EnsureFromOpinionAsync(PunditOpinion opinion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(opinion.MatchId))
        {
            return;
        }

        if (!MatchResolutionService.IsMatchLevelPrediction(opinion.PredictionType))
        {
            return;
        }

        var predictionText = opinion.Prediction ?? opinion.Opinion;
        if (string.IsNullOrWhiteSpace(predictionText))
        {
            return;
        }

        var pundit = opinion.Pundit ?? await db.Pundits.FindAsync([opinion.PunditId], cancellationToken);
        if (pundit is null || pundit.Kind != PunditKind.Source)
        {
            return;
        }

        if (!await db.Matches.AnyAsync(m => m.Id == opinion.MatchId, cancellationToken))
        {
            return;
        }

        var item = opinion.SourceItem;
        if (item is null)
        {
            item = await db.MediaItems
                .Include(i => i.MediaSource)
                .FirstOrDefaultAsync(i => i.Id == opinion.SourceItemId, cancellationToken);
        }
        else if (item.MediaSource is null)
        {
            await db.Entry(item).Reference(i => i.MediaSource).LoadAsync(cancellationToken);
        }

        var existing = await db.PunditPredictions.FirstOrDefaultAsync(
            p => p.PunditId == pundit.Id &&
                 p.MatchId == opinion.MatchId &&
                 p.PredictionType == opinion.PredictionType,
            cancellationToken);

        var predictedScore = ExtractScore(predictionText);

        if (existing is null)
        {
            db.PunditPredictions.Add(new PunditPrediction
            {
                Id = Guid.NewGuid(),
                PunditId = pundit.Id,
                MatchId = opinion.MatchId,
                Prediction = predictionText,
                PublishedAt = item?.PublishedAt ?? opinion.CreatedAt,
                SourceType = item?.MediaSource?.SourceType,
                SourceUrl = item?.SourceUrl,
                Author = pundit.Name,
                Speaker = pundit.Name,
                PredictionType = opinion.PredictionType,
                PredictedTeam = opinion.Team,
                PredictedScore = predictedScore,
                Confidence = opinion.Confidence,
                EvidenceSnippet = opinion.EvidenceQuote,
                IsMatched = true
            });
            return;
        }

        existing.Prediction = predictionText;
        existing.SourceUrl = item?.SourceUrl ?? existing.SourceUrl;
        existing.PredictedTeam = opinion.Team ?? existing.PredictedTeam;
        existing.PredictedScore = predictedScore ?? existing.PredictedScore;
        existing.Confidence = opinion.Confidence ?? existing.Confidence;
        existing.EvidenceSnippet = opinion.EvidenceQuote ?? existing.EvidenceSnippet;
        existing.IsMatched = true;
    }

    private static string? ExtractScore(string? prediction)
    {
        if (string.IsNullOrWhiteSpace(prediction))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(prediction, @"\b(\d+)\s*[-–]\s*(\d+)\b");
        return match.Success ? match.Value : null;
    }
}
