using BanterApp.Api.Common;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Media.Dtos;
using BanterApp.Api.Integrations.Rss;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.News;

/// <summary>
/// Pulls headlines from RSS feeds (BBC, ESPN, FIFA, etc.) without an API key.
/// Prefers the <c>rss_feeds</c> catalog when seeded; falls back to config URLs.
/// </summary>
public sealed class RssNewsProvider
{
    private readonly IRssFeedProvider _rss;
    private readonly NewsOptions _options;
    private readonly IServiceScopeFactory _scopes;

    public RssNewsProvider(
        IRssFeedProvider rss,
        IOptions<NewsOptions> options,
        IServiceScopeFactory scopes)
    {
        _rss = rss;
        _options = options.Value;
        _scopes = scopes;
    }

    public async Task<IReadOnlyList<NewsArticleDto>> GetLatestArticlesAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        var feeds = await ResolveNewsFeedsAsync(cancellationToken);
        if (feeds.Count == 0)
        {
            return [];
        }

        var perFeed = Math.Max(3, count / feeds.Count);
        var articles = new List<NewsArticleDto>();

        foreach (var feed in feeds)
        {
            var items = await _rss.FetchFeedAsync(feed.Url, perFeed, cancellationToken);
            foreach (var item in items)
            {
                articles.Add(MapItem(item, feed.Url, feed.Name));
            }
        }

        return articles
            .OrderByDescending(a => a.PublishedAt)
            .Take(count)
            .ToList();
    }

    private async Task<IReadOnlyList<(string Name, string Url)>> ResolveNewsFeedsAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<IRssFeedCatalog>();
        var fromDb = await catalog.GetActiveForNewsAsync(ct);
        if (fromDb.Count > 0)
        {
            return fromDb
                .Select(f => (f.Name, Url: f.RssUrl.Trim()))
                .ToList();
        }

        return _options.RssFeedUrls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => (Name: SourceNameFromFeed(u.Trim()), Url: u.Trim()))
            .ToList();
    }

    private static NewsArticleDto MapItem(MediaItemDto item, string feedUrl, string sourceName)
    {
        var id = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(item.SourceUrl))[..16];

        return new NewsArticleDto(
            Id: $"rss-{id}",
            Title: item.Title,
            Summary: item.Description ?? item.Title,
            SourceName: string.IsNullOrWhiteSpace(sourceName) ? SourceNameFromFeed(feedUrl) : sourceName,
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
