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

    /// <summary>
    /// When true, paraphrased (non-verbatim) opinions can auto-publish provided they
    /// meet <see cref="AutoApproveConfidence"/>. When false, only direct quotes auto-publish.
    /// </summary>
    public bool AllowParaphrase { get; set; } = true;

    /// <summary>
    /// Confidence at/above which an otherwise-clean opinion auto-publishes even if it is a
    /// paraphrase. Should be >= a sensible floor; independent of <see cref="MinConfidenceWithoutReview"/>.
    /// </summary>
    public double AutoApproveConfidence { get; set; } = 0.55;

    /// <summary>
    /// When true, opinions tagged as "general_opinion" are sent to human review. When false
    /// they can auto-publish if they otherwise pass the checks.
    /// </summary>
    public bool FlagGeneralOpinion { get; set; } = false;
}
