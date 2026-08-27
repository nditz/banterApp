using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Fetches reaction GIFs from the Giphy API. Returns persistable <c>media*.giphy.com</c> CDN URLs.
/// </summary>
public sealed class GiphyGifProvider : IReactionGifProvider
{
    private readonly HttpClient _httpClient;
    private readonly ReactionGifOptions _options;
    private readonly ILogger<GiphyGifProvider> _logger;

    private readonly ConcurrentDictionary<string, string[]> _cache = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxCacheEntries = 500;

    public GiphyGifProvider(
        HttpClient httpClient,
        IOptions<ReactionGifOptions> options,
        ILogger<GiphyGifProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.IsGiphyEnabled;

    public async Task<string?> FindGifUrlAsync(
        string query,
        int seed,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var normalized = query.Trim();
        var offset = (int)((uint)seed % 25);
        var cacheKey = $"{normalized}|{offset}";
        if (!_cache.TryGetValue(cacheKey, out var urls))
        {
            urls = await FetchAsync(normalized, offset, cancellationToken);
            if (urls.Length > 0 && _cache.Count < MaxCacheEntries)
            {
                _cache.TryAdd(cacheKey, urls);
            }
        }

        if (urls.Length == 0)
        {
            return null;
        }

        var index = (int)((uint)seed % (uint)urls.Length);
        return urls[index];
    }

    private async Task<string[]> FetchAsync(string query, int offset, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(_options.SearchLimit, 1, 50);
        var rating = string.IsNullOrWhiteSpace(_options.ContentRating) ? "pg" : _options.ContentRating;
        var url =
            $"{_options.GiphyBaseUrl.TrimEnd('/')}/gifs/search" +
            $"?api_key={Uri.EscapeDataString(_options.ApiKey!)}" +
            $"&q={Uri.EscapeDataString(query)}" +
            $"&limit={limit}" +
            $"&offset={offset}" +
            $"&rating={Uri.EscapeDataString(rating)}" +
            "&lang=en";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Giphy search failed: {Status} {Body}",
                    (int)response.StatusCode,
                    body);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return GiphyResponseParser.ExtractGifUrls(doc.RootElement);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Giphy search request errored for query '{Query}'.", query);
            return [];
        }
    }
}

public static class GiphyResponseParser
{
    public static string[] ExtractGifUrls(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<string>();
        foreach (var item in data.EnumerateArray())
        {
            if (!item.TryGetProperty("images", out var images))
            {
                continue;
            }

            var gifUrl = ExtractImageUrl(images, "original")
                ?? ExtractImageUrl(images, "fixed_height")
                ?? ExtractImageUrl(images, "downsized");

            if (!string.IsNullOrWhiteSpace(gifUrl))
            {
                found.Add(gifUrl);
            }
        }

        return found.ToArray();
    }

    private static string? ExtractImageUrl(JsonElement images, string formatName) =>
        images.TryGetProperty(formatName, out var format) &&
        format.TryGetProperty("url", out var urlEl)
            ? urlEl.GetString()
            : null;
}
