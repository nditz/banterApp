using System.Net.Http.Headers;
using System.Text.Json;
using BanterApp.Api.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.News;

/// <summary>
/// NewsAPI.org provider skeleton.
/// Falls back to <see cref="MockNewsProvider"/> when the API key is missing or requests fail.
/// </summary>
public sealed class NewsApiProvider : INewsProvider
{
    private const string BaseUrl = "https://newsapi.org/v2";

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly MockNewsProvider _fallback;
    private readonly ILogger<NewsApiProvider> _logger;

    public NewsApiProvider(
        HttpClient httpClient,
        IOptions<NewsOptions> options,
        ILogger<NewsApiProvider> logger)
    {
        _httpClient = httpClient;
        _apiKey = options.Value.ApiKey;
        _fallback = new MockNewsProvider();
        _logger = logger;
    }

    public async Task<IReadOnlyList<NewsArticleDto>> GetLatestArticlesAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogDebug("News:ApiKey not set; using mock news provider.");
            return await _fallback.GetLatestArticlesAsync(count, cancellationToken);
        }

        try
        {
            var url =
                $"{BaseUrl}/everything?q=Premier+League&language=en&sortBy=publishedAt&pageSize={Math.Clamp(count, 1, 100)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NewsAPI returned {StatusCode}; using mock data.", response.StatusCode);
                return await _fallback.GetLatestArticlesAsync(count, cancellationToken);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var articles = MapArticles(document);
            return articles.Count > 0
                ? articles
                : await _fallback.GetLatestArticlesAsync(count, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NewsAPI request failed; using mock data.");
            return await _fallback.GetLatestArticlesAsync(count, cancellationToken);
        }
    }

    private static IReadOnlyList<NewsArticleDto> MapArticles(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("articles", out var articlesElement))
        {
            return [];
        }

        var results = new List<NewsArticleDto>();

        foreach (var article in articlesElement.EnumerateArray())
        {
            var title = article.GetProperty("title").GetString();
            var url = article.GetProperty("url").GetString();
            var sourceName = article.GetProperty("source").GetProperty("name").GetString();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(sourceName))
            {
                continue;
            }

            var author = article.TryGetProperty("author", out var authorEl) ? authorEl.GetString() : null;
            var description = article.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;
            var imageUrl = article.TryGetProperty("urlToImage", out var imgEl) ? imgEl.GetString() : null;

            DateTimeOffset publishedAt = DateTimeOffset.UtcNow;
            if (article.TryGetProperty("publishedAt", out var pubEl) &&
                DateTimeOffset.TryParse(pubEl.GetString(), out var parsed))
            {
                publishedAt = PostgresUtc.Normalize(parsed);
            }

            results.Add(new NewsArticleDto(
                Id: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(url))[..12],
                Title: title,
                Summary: description ?? title,
                SourceName: sourceName,
                SourceUrl: url,
                Author: author,
                PublishedAt: publishedAt,
                ImageUrl: imageUrl,
                Category: "News"));
        }

        return results;
    }
}
