namespace BanterApp.Api.Integrations.Pundits;

public sealed class PunditIngestOptions
{
    public const string SectionName = "PunditIngest";

    public bool Enabled { get; set; } = true;

    public string[] YouTubeSearchQueries { get; set; } = [];

    public string[] RssFeedUrls { get; set; } = [];

    public int MaxItemsPerSource { get; set; } = 25;

    public int ExtractionBatchSize { get; set; } = 5;

    public double MinConfidenceWithoutReview { get; set; } = 0.6;

    public bool FetchArticleBodies { get; set; } = true;

    public int MinSourceTextLength { get; set; } = 200;
}
