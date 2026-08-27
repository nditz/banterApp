using BanterApp.Api.Common;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Media.Dtos;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.News;

/// <summary>
/// Pulls headlines from RSS feeds (BBC, ESPN, FIFA, etc.) without an API key.
/// </summary>
public sealed class RssNewsProvider
{
    private readonly IRssFeedProvider _rss;
    private readonly NewsOptions _options;

    public RssNewsProvider(IRssFeedProvider rss, IOptions<NewsOptions> options)
    {
        _rss = rss;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<NewsArticleDto>> GetLatestArticlesAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        if (_options.RssFeedUrls.Length == 0)
        {
            return [];
        }

        var perFeed = Math.Max(3, count / _options.RssFeedUrls.Length);
        var articles = new List<NewsArticleDto>();

        foreach (var feedUrl in _options.RssFeedUrls)
        {
            var items = await _rss.FetchFeedAsync(feedUrl, perFeed, cancellationToken);
            foreach (var item in items)
            {
                articles.Add(MapItem(item, feedUrl));
            }
        }

        return articles
            .OrderByDescending(a => a.PublishedAt)
            .Take(count)
            .ToList();
    }

    private static NewsArticleDto MapItem(MediaItemDto item, string feedUrl)
    {
        var sourceName = SourceNameFromFeed(feedUrl);
        var id = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(item.SourceUrl))[..16];

        return new NewsArticleDto(
            Id: $"rss-{id}",
            Title: item.Title,
            Summary: item.Description ?? item.Title,
            SourceName: sourceName,
            SourceUrl: item.SourceUrl,
            Author: null,
            PublishedAt: PostgresUtc.Normalize(item.PublishedAt ?? DateTimeOffset.UtcNow),
            ImageUrl: null,
            Category: "sports_news");
    }

    private static string SourceNameFromFeed(string feedUrl) =>
        feedUrl switch
        {
            var u when u.Contains("bbci.co.uk", StringComparison.OrdinalIgnoreCase) => "BBC Sport",
            var u when u.Contains("espn.com", StringComparison.OrdinalIgnoreCase) => "ESPN",
            var u when u.Contains("theguardian.com", StringComparison.OrdinalIgnoreCase) => "The Guardian",
            var u when u.Contains("fifa.com", StringComparison.OrdinalIgnoreCase) => "FIFA",
            _ => "Sports RSS"
        };
}
