using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;

namespace BanterApp.Api.Integrations.Rss;

public interface IRssFeedCatalogSeed
{
    IReadOnlyList<RssFeedSeedEntry> Feeds { get; }
}

public sealed class RssFeedSeedEntry
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = RssFeedKind.Website;
    public string RssUrl { get; set; } = string.Empty;
    public long? ApplePodcastId { get; set; }
    public string? SiteUrl { get; set; }
    public string? StyleSlug { get; set; }
    public double SourceWeight { get; set; } = 1.0;
    public bool ExtractPredictions { get; set; } = true;
    public bool UseForMediaIngest { get; set; }
    public bool UseForNews { get; set; }
    public bool UseForPundit { get; set; }
    public ConfidenceScoringOptions? ConfidenceScoring { get; set; }
}

public sealed class StaticRssFeedCatalogSeed(IReadOnlyList<RssFeedSeedEntry>? feeds = null) : IRssFeedCatalogSeed
{
    public IReadOnlyList<RssFeedSeedEntry> Feeds { get; } = feeds ?? [];
}
