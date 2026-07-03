using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Fetches reaction GIFs from the Tenor (Google) API. Returns stable <c>media.tenor.com</c> CDN
/// URLs (from <c>media_formats.gif.url</c>) that are safe to persist on feed items.
/// </summary>
public sealed class TenorGifProvider : IReactionGifProvider
{
    private readonly HttpClient _httpClient;
    private readonly ReactionGifOptions _options;
    private readonly ILogger<TenorGifProvider> _logger;

    // Query -> candidate GIF URLs, cached for the process lifetime to limit API calls.
    private readonly ConcurrentDictionary<string, string[]> _cache = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxCacheEntries = 500;

    public TenorGifProvider(
        HttpClient httpClient,
        IOptions<ReactionGifOptions> options,
        ILogger<TenorGifProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.IsTenorEnabled;

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
        if (!_cache.TryGetValue(normalized, out var urls))
        {
            urls = await FetchAsync(normalized, cancellationToken);
            if (urls.Length > 0 && _cache.Count < MaxCacheEntries)
            {
                _cache.TryAdd(normalized, urls);
            }
        }

        if (urls.Length == 0)
        {
            return null;
        }

        var index = (int)((uint)seed % (uint)urls.Length);
        return urls[index];
    }

    private async Task<string[]> FetchAsync(string query, CancellationToken cancellationToken)
    {
        var url =
            $"{_options.TenorBaseUrl.TrimEnd('/')}/search" +
            $"?q={Uri.EscapeDataString(query)}" +
            $"&key={Uri.EscapeDataString(_options.ApiKey!)}" +
            $"&client_key={Uri.EscapeDataString(_options.ClientKey)}" +
            $"&limit={Math.Clamp(_options.SearchLimit, 1, 50)}" +
            $"&contentfilter={Uri.EscapeDataString(_options.ContentFilter)}" +
            "&media_filter=gif";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Tenor search failed: {Status} {Body}",
                    (int)response.StatusCode,
                    body);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var found = new List<string>();
            foreach (var result in results.EnumerateArray())
            {
                if (!result.TryGetProperty("media_formats", out var formats))
                {
                    continue;
                }

                var gifUrl = ExtractFormatUrl(formats, "gif")
                    ?? ExtractFormatUrl(formats, "mediumgif")
                    ?? ExtractFormatUrl(formats, "tinygif");

                if (!string.IsNullOrWhiteSpace(gifUrl))
                {
                    found.Add(gifUrl);
                }
            }

            return found.ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Tenor search request errored for query '{Query}'.", query);
            return [];
        }
    }

    private static string? ExtractFormatUrl(JsonElement formats, string formatName) =>
        formats.TryGetProperty(formatName, out var format) &&
        format.TryGetProperty("url", out var urlEl)
            ? urlEl.GetString()
            : null;
}
