namespace BanterApp.Api.Integrations.News;

public sealed class NewsIngestOptions
{
    public const string SectionName = "NewsIngest";

    public bool Enabled { get; set; } = true;

    public int MaxArticlesPerRun { get; set; } = 25;

    /// <summary>Pull upcoming fixtures into the rolling news feed.</summary>
    public bool IncludeMatchFixtures { get; set; } = true;

    /// <summary>Pull full-time results into the rolling news feed.</summary>
    public bool IncludeMatchResults { get; set; } = true;

    /// <summary>Pull live in-play scores into the rolling news feed.</summary>
    public bool IncludeLiveScores { get; set; } = true;

    /// <summary>Future: YouTube channel IDs for transcript scraping.</summary>
    public string[] YouTubeChannelIds { get; set; } = [];

    /// <summary>Future: podcast RSS feed URLs for transcript ingestion.</summary>
    public string[] PodcastFeedUrls { get; set; } = [];
}
