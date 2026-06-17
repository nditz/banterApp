using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.News;

/// <summary>
/// Combines NewsAPI.org (when keyed) with free RSS sports feeds.
/// </summary>
public sealed class CompositeNewsProvider : INewsProvider
{
    private readonly NewsApiProvider? _newsApi;
    private readonly RssNewsProvider _rss;
    private readonly NewsOptions _options;

    public CompositeNewsProvider(
        IServiceProvider services,
        RssNewsProvider rss,
        IOptions<NewsOptions> options)
    {
        _rss = rss;
        _options = options.Value;
        _newsApi = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? null
            : services.GetRequiredService<NewsApiProvider>();
    }

    public async Task<IReadOnlyList<NewsArticleDto>> GetLatestArticlesAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var merged = new Dictionary<string, NewsArticleDto>(StringComparer.OrdinalIgnoreCase);

        var rss = await _rss.GetLatestArticlesAsync(count, cancellationToken);
        foreach (var article in rss)
        {
            merged[article.Id] = article;
        }

        if (_newsApi is not null)
        {
            var apiArticles = await _newsApi.GetLatestArticlesAsync(count, cancellationToken);
            foreach (var article in apiArticles)
            {
                merged[article.Id] = article;
            }
        }

        return merged.Values
            .OrderByDescending(a => a.PublishedAt)
            .Take(count)
            .ToList();
    }
}
