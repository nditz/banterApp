using System.Text.Json;
using BanterApp.Api.Integrations.Ai;

namespace BanterApp.Api.Integrations.FootballBanter;

public interface IFootballBanterEngine
{
    Task<FootballBanterOutput> GenerateAsync(
        FootballBanterSourceInput input,
        CancellationToken cancellationToken = default);
}

public sealed class FootballBanterEngine : IFootballBanterEngine
{
    private readonly IFootballBanterConfigProvider _config;
    private readonly IContentGenerator _contentGenerator;
    private readonly ILogger<FootballBanterEngine> _logger;

    public FootballBanterEngine(
        IFootballBanterConfigProvider config,
        IContentGenerator contentGenerator,
        ILogger<FootballBanterEngine> logger)
    {
        _config = config;
        _contentGenerator = contentGenerator;
        _logger = logger;
    }

    public async Task<FootballBanterOutput> GenerateAsync(
        FootballBanterSourceInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(input);

        if (!_config.Config.OpenAi.Enabled)
        {
            return BuildStubOutput(input);
        }

        try
        {
            var json = await _contentGenerator.GenerateFootballBanterJsonAsync(
                input,
                _config.SystemPrompt,
                _config.Config.OpenAi,
                _config.Config.Banter.DefaultIntensity,
                cancellationToken);

            var parsed = FootballBanterOutputParser.TryParse(json);
            if (parsed is null)
            {
                _logger.LogWarning("Football Banter Engine returned invalid JSON; using stub fallback.");
                return BuildStubOutput(input);
            }

            return FinalizeOutput(parsed, input);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Football Banter Engine generation failed; using stub fallback.");
            return BuildStubOutput(input);
        }
    }

    internal FootballBanterOutput FinalizeOutput(FootballBanterOutput output, FootballBanterSourceInput input)
    {
        if (string.IsNullOrWhiteSpace(output.SourceName))
        {
            output.SourceName = input.SourceName;
        }

        if (string.IsNullOrWhiteSpace(output.SourceUrl))
        {
            output.SourceUrl = input.SourceUrl;
        }

        if (string.IsNullOrWhiteSpace(output.PunditName) && !string.IsNullOrWhiteSpace(input.PunditName))
        {
            output.PunditName = input.PunditName;
        }

        if (string.IsNullOrWhiteSpace(output.Prediction) && !string.IsNullOrWhiteSpace(input.Prediction))
        {
            output.Prediction = input.Prediction;
        }

        if (output.Confidence <= 0 && input.Confidence is > 0)
        {
            output.Confidence = input.Confidence.Value;
        }

        if (input.StatementType is not null)
        {
            output.StatementType = input.StatementType.Value;
        }

        ApplyReviewRules(output, input);
        return output;
    }

    private void ApplyReviewRules(FootballBanterOutput output, FootballBanterSourceInput input)
    {
        var rules = _config.Config.ReviewRules.NeedsHumanReviewWhen;
        var threshold = FootballBanterDefaults.ReviewConfidenceThreshold;

        if (rules.Contains("source_url_missing", StringComparer.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(output.SourceUrl))
        {
            output.NeedsHumanReview = true;
        }

        if (rules.Contains("pundit_name_missing", StringComparer.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(output.PunditName) &&
            string.Equals(input.SourceType, "youtube", StringComparison.OrdinalIgnoreCase))
        {
            output.NeedsHumanReview = true;
        }

        if (rules.Contains("source_text_incomplete", StringComparer.OrdinalIgnoreCase) &&
            input.SourceText.Trim().Length < 40)
        {
            output.NeedsHumanReview = true;
        }

        if (rules.Contains("quote_is_inferred_not_direct", StringComparer.OrdinalIgnoreCase) &&
            output.StatementType == FootballBanterStatementType.InferredPrediction)
        {
            output.NeedsHumanReview = true;
        }

        if (rules.Contains("confidence_below_0_7", StringComparer.OrdinalIgnoreCase) &&
            output.Confidence < threshold)
        {
            output.NeedsHumanReview = true;
        }

        if (rules.Contains("prediction_is_vague", StringComparer.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(output.Prediction) &&
            output.Prediction.Trim().Length < 8)
        {
            output.NeedsHumanReview = true;
        }

        if (string.IsNullOrWhiteSpace(output.Headline) || string.IsNullOrWhiteSpace(output.BanterSummary))
        {
            output.NeedsHumanReview = true;
        }
    }

    private static void ValidateInput(FootballBanterSourceInput input)
    {
        if (string.IsNullOrWhiteSpace(input.SourceName))
        {
            throw new ArgumentException("source_name is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.SourceUrl))
        {
            throw new ArgumentException("source_url is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.SourceText))
        {
            throw new ArgumentException("source_text is required.", nameof(input));
        }
    }

    private FootballBanterOutput BuildStubOutput(FootballBanterSourceInput input) =>
        FinalizeOutput(FootballBanterStubOutputBuilder.Build(input), input);
}
