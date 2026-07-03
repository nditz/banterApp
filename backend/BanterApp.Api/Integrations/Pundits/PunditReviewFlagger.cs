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
        int distinctPunditCount,
        string? role = null)
    {
        if (opinion.NeedsHumanReview)
        {
            return true;
        }

        var isJournalist = IsJournalistRole(role);

        // Always review when we can't attribute the take to a named pundit.
        if (string.IsNullOrWhiteSpace(punditName) ||
            string.Equals(punditName, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return !isJournalist;
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
            if (isJournalist && opinion.Confidence >= _options.AutoApproveConfidence)
            {
                return false;
            }

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

    private static bool IsJournalistRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return role.Contains("journalist", StringComparison.OrdinalIgnoreCase) ||
               role.Contains("reporter", StringComparison.OrdinalIgnoreCase) ||
               role.Contains("columnist", StringComparison.OrdinalIgnoreCase);
    }
}
