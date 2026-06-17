namespace BanterApp.Api.Integrations.News;

public sealed class NewsOptions
{
    public const string SectionName = "News";

    public string? ApiKey { get; set; }

    /// <summary>Free RSS feeds used when NewsAPI key is absent or as a supplement.</summary>
    public string[] RssFeedUrls { get; set; } = [];
}
