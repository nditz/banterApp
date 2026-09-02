using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Rss;

namespace BanterApp.Api.Integrations.Pundits;

public static class ConfidenceScoringHelper
{
    public static double AdjustConfidence(
        double baseConfidence,
        string? text,
        ConfidenceScoringOptions? options)
    {
        if (options is null || !options.Enable || string.IsNullOrWhiteSpace(text))
        {
            return baseConfidence;
        }

        var adjusted = baseConfidence;
        var normalized = text.ToLowerInvariant();

        foreach (var keyword in options.Keywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) &&
                normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                adjusted += 0.05;
            }
        }

        foreach (var keyword in options.LowConfidence)
        {
            if (!string.IsNullOrWhiteSpace(keyword) &&
                normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                adjusted -= 0.08;
            }
        }

        return Math.Clamp(adjusted, 0, 1);
    }

    public static ConfidenceScoringOptions? ResolveForSource(
        MediaSource source,
        IRssFeedCatalogSeed seed)
    {
        foreach (var feed in seed.Feeds)
        {
            if (Matches(source, feed))
            {
                return feed.ConfidenceScoring;
            }
        }

        return null;
    }

    public static double ResolveSourceWeight(MediaSource source, IRssFeedCatalogSeed seed)
    {
        foreach (var feed in seed.Feeds)
        {
            if (Matches(source, feed))
            {
                return feed.SourceWeight;
            }
        }

        return source.SourceType switch
        {
            "podcast" => 1.2,
            "youtube" => 0.8,
            _ => 1.0
        };
    }

    private static bool Matches(MediaSource source, RssFeedSeedEntry feed) =>
        string.Equals(source.Name, feed.Name, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(source.RssUrl, feed.RssUrl, StringComparison.OrdinalIgnoreCase);
}
