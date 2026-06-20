namespace BanterApp.Api.Integrations.Media;

public sealed class MediaIngestOptions
{
    public const string SectionName = "MediaIngest";

    public bool Enabled { get; set; } = true;

    public int MaxItemsPerSource { get; set; } = 10;

    /// <summary>Legacy: channel IDs only (display name defaults to YouTube:{id}). Prefer <see cref="YouTubeChannels"/>.</summary>
    public string[] YouTubeChannelIds { get; set; } = [];

    /// <summary>Named YouTube sources — required for licensed attribution when real takes are cited.</summary>
    public YouTubeChannelConfig[] YouTubeChannels { get; set; } = [];

    /// <summary>Legacy: RSS URLs only. Prefer <see cref="PodcastSources"/> for outlet name + attribution.</summary>
    public string[] PodcastFeedUrls { get; set; } = [];

    /// <summary>Named podcast RSS sources for pundit prediction / soundbite extraction.</summary>
    public PodcastSourceConfig[] PodcastSources { get; set; } = [];

    public WebsiteSourceConfig[] WebsiteSources { get; set; } = [];
}

public sealed class YouTubeChannelConfig
{
    /// <summary>Outlet or show name shown in feed attribution, e.g. "CBS Sports Golazo".</summary>
    public string Name { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;

    public string? SiteUrl { get; set; }

    /// <summary>Optional desk slug matching <c>PunditPersonas</c> for compare-vs-pro UX.</summary>
    public string? StyleSlug { get; set; }

    public bool ExtractPredictions { get; set; } = true;
}

public sealed class PodcastSourceConfig
{
    public string Name { get; set; } = string.Empty;

    public string RssUrl { get; set; } = string.Empty;

    public string? SiteUrl { get; set; }

    public string? StyleSlug { get; set; }

    public bool ExtractPredictions { get; set; } = true;
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
