using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media;

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
        MediaIngestOptions mediaOptions)
    {
        foreach (var podcast in mediaOptions.PodcastSources)
        {
            if (string.Equals(source.Name, podcast.Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.RssUrl, podcast.RssUrl, StringComparison.OrdinalIgnoreCase))
            {
                return podcast.ConfidenceScoring;
            }
        }

        foreach (var website in mediaOptions.WebsiteSources)
        {
            if (string.Equals(source.Name, website.Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.RssUrl, website.RssUrl, StringComparison.OrdinalIgnoreCase))
            {
                return website.ConfidenceScoring;
            }
        }

        return null;
    }

    public static double ResolveSourceWeight(MediaSource source, MediaIngestOptions mediaOptions)
    {
        foreach (var podcast in mediaOptions.PodcastSources)
        {
            if (string.Equals(source.Name, podcast.Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.RssUrl, podcast.RssUrl, StringComparison.OrdinalIgnoreCase))
            {
                return podcast.SourceWeight;
            }
        }

        foreach (var website in mediaOptions.WebsiteSources)
        {
            if (string.Equals(source.Name, website.Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.RssUrl, website.RssUrl, StringComparison.OrdinalIgnoreCase))
            {
                return website.SourceWeight;
            }
        }

        return source.SourceType switch
        {
            "podcast" => 1.2,
            "youtube" => 0.8,
            _ => 1.0
        };
    }
}
