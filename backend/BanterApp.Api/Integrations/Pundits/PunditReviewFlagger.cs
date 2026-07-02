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

        // Always review when we can't attribute the take to a named pundit.
        if (string.IsNullOrWhiteSpace(punditName) ||
            string.Equals(punditName, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Paraphrases: only auto-publish when allowed AND confidence is high enough.
        if (!opinion.IsDirectQuote)
        {
            if (!_options.AllowParaphrase || opinion.Confidence < _options.AutoApproveConfidence)
            {
                return true;
            }
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

        // "general_opinion" takes are optionally gated behind review.
        if (_options.FlagGeneralOpinion &&
            string.Equals(opinion.PredictionType, "general_opinion", StringComparison.OrdinalIgnoreCase))
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
