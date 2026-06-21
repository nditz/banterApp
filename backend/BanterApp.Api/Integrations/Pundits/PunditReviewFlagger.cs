using BanterApp.Api.Integrations.Pundits.Dtos;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class PunditReviewFlagger
{
    private readonly PunditIngestOptions _options;

    public PunditReviewFlagger(IOptions<PunditIngestOptions> options)
    {
        _options = options.Value;
    }

    public bool ShouldReviewOpinion(
        PunditExtractionOpinionDto opinion,
        string punditName,
        int sourceTextLength,
        int distinctPunditCount)
    {
        if (opinion.NeedsHumanReview)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(punditName) ||
            string.Equals(punditName, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!opinion.IsDirectQuote)
        {
            return true;
        }

        if (sourceTextLength < _options.MinSourceTextLength)
        {
            return true;
        }

        if (opinion.Confidence < _options.MinConfidenceWithoutReview)
        {
            return true;
        }

        if (distinctPunditCount > 3)
        {
            return true;
        }

        if (string.Equals(opinion.PredictionType, "general_opinion", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(opinion.Prediction) &&
            opinion.Prediction.Length < 8 &&
            !opinion.Prediction.Contains(' ', StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
