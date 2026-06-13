namespace BanterApp.Api.Integrations.Media;

public sealed class MediaIngestOptions
{
    public const string SectionName = "MediaIngest";

    public bool Enabled { get; set; } = true;

    public int MaxItemsPerSource { get; set; } = 10;

    public string[] YouTubeChannelIds { get; set; } = [];

    public string[] PodcastFeedUrls { get; set; } = [];

    public WebsiteSourceConfig[] WebsiteSources { get; set; } = [];
}

public sealed class WebsiteSourceConfig
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "website";
    public string? RssUrl { get; set; }
    public string? BaseUrl { get; set; }
    public string? RobotsUrl { get; set; }
    public bool? CrawlAllowed { get; set; }
    public bool ExtractPredictions { get; set; } = true;
}
