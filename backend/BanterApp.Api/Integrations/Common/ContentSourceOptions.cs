namespace BanterApp.Api.Integrations.Common;

public sealed class ConfidenceScoringOptions
{
    public bool Enable { get; set; } = true;

    public string[] Keywords { get; set; } =
    [
        "certainly", "definitely", "will", "guaranteed", "surely"
    ];

    public string[] LowConfidence { get; set; } =
    [
        "maybe", "possibly", "might", "could", "uncertain"
    ];
}

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";

    public PredictionExtractionProcessingOptions PredictionExtraction { get; set; } = new();

    public SentimentAnalysisOptions SentimentAnalysis { get; set; } = new();

    public AggregationProcessingOptions Aggregation { get; set; } = new();
}

public sealed class PredictionExtractionProcessingOptions
{
    public bool EnableNlp { get; set; } = true;

    public double ConfidenceThreshold { get; set; } = 0.6;

    public int MaxPredictionsPerSource { get; set; } = 20;

    public bool StoreHistorical { get; set; } = true;

    public int CacheDuration { get; set; } = 3600;

    public int MinBanterQualityScore { get; set; } = 70;

    public int MinFeedQualityScore { get; set; } = 60;
}

public sealed class SentimentAnalysisOptions
{
    public bool Enable { get; set; } = true;

    public string Provider { get; set; } = "openai";

    public string Model { get; set; } = "gpt-4o-mini";

    public int BatchSize { get; set; } = 10;
}

public sealed class AggregationProcessingOptions
{
    public bool WeightedScoring { get; set; } = true;

    public double ConsensusThreshold { get; set; } = 0.7;

    public double RecentWeighting { get; set; } = 1.2;
}

public sealed class SourceWeightsOptions
{
    public const string SectionName = "SourceWeights";

    public Dictionary<string, double> DefaultWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["podcast"] = 1.2,
        ["website"] = 1.0,
        ["twitter"] = 1.1,
        ["reddit"] = 0.6,
        ["youtube"] = 0.8,
        ["betting"] = 1.5
    };

    public Dictionary<string, double> ConfidenceMultiplier { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ex-pro-couch"] = 1.3,
        ["silky-studio"] = 1.1,
        ["analytical-modern"] = 1.2,
        ["bbc-studio"] = 1.1,
        ["studio-panel"] = 1.0
    };
}
