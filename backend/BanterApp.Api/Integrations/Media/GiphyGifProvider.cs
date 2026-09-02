using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Fetches reaction GIFs from Giphy with weekend uniqueness. Prefers <c>/gifs/random</c>
/// (Giphy's variety endpoint), then shuffled search pages, and refuses GIFs already claimed
/// in the current Friday–Monday window.
/// </summary>
public sealed class GiphyGifProvider : IReactionGifProvider
{
    private readonly HttpClient _httpClient;
    private readonly ReactionGifOptions _options;
    private readonly IReactionGifLedger _ledger;
    private readonly ILogger<GiphyGifProvider> _logger;

    public GiphyGifProvider(
        HttpClient httpClient,
        IOptions<ReactionGifOptions> options,
        IReactionGifLedger ledger,
        ILogger<GiphyGifProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _ledger = ledger;
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

        var assigned = await _ledger.GetAssignedUrlAsync(seed, cancellationToken);
        if (!string.IsNullOrWhiteSpace(assigned))
        {
            return assigned;
        }

        var normalized = GiphyGifSelector.TruncateQuery(query);

        for (var attempt = 0; attempt < GiphyGifSelector.RandomAttempts; attempt++)
        {
            var hit = await FetchRandomAsync(normalized, cancellationToken);
            if (hit is not null && await TryClaimAsync(seed, hit, cancellationToken))
            {
                return hit.Url;
            }
        }

        var pageSize = Math.Clamp(_options.SearchLimit, 1, 50);
        for (var attempt = 0; attempt < GiphyGifSelector.SearchAttempts; attempt++)
        {
            var offset = attempt == 0 ? GiphyGifSelector.RandomSearchOffset() : 0;
            var hits = await FetchSearchAsync(normalized, offset, pageSize, cancellationToken);
            GiphyGifSelector.Shuffle(hits);
            foreach (var hit in hits)
            {
                if (await TryClaimAsync(seed, hit, cancellationToken))
                {
                    return hit.Url;
                }
            }
        }

        var fallback = await FetchRandomAsync(normalized, cancellationToken);
        if (fallback is not null)
        {
            await TryClaimAsync(seed, fallback, cancellationToken);
            return fallback.Url;
        }

        return null;
    }

    private async Task<bool> TryClaimAsync(int seed, GiphyGifHit hit, CancellationToken cancellationToken) =>
        await _ledger.TryClaimAsync(seed, hit.Id, hit.Url, cancellationToken);

    private async Task<GiphyGifHit?> FetchRandomAsync(string query, CancellationToken cancellationToken)
    {
        var rating = string.IsNullOrWhiteSpace(_options.ContentRating) ? "pg" : _options.ContentRating;
        var url =
            $"{_options.GiphyBaseUrl.TrimEnd('/')}/gifs/random" +
            $"?api_key={Uri.EscapeDataString(_options.ApiKey!)}" +
            $"&tag={Uri.EscapeDataString(query)}" +
            $"&rating={Uri.EscapeDataString(rating)}";

        var hits = await GetHitsAsync(url, query, "random", cancellationToken);
        return hits.Count == 0 ? null : hits[0];
    }

    private async Task<List<GiphyGifHit>> FetchSearchAsync(
        string query,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var rating = string.IsNullOrWhiteSpace(_options.ContentRating) ? "pg" : _options.ContentRating;
        var url =
            $"{_options.GiphyBaseUrl.TrimEnd('/')}/gifs/search" +
            $"?api_key={Uri.EscapeDataString(_options.ApiKey!)}" +
            $"&q={Uri.EscapeDataString(query)}" +
            $"&limit={limit}" +
            $"&offset={offset}" +
            $"&rating={Uri.EscapeDataString(rating)}" +
            "&lang=en";

        return await GetHitsAsync(url, query, "search", cancellationToken);
    }

    private async Task<List<GiphyGifHit>> GetHitsAsync(
        string url,
        string query,
        string kind,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Giphy {Kind} failed: {Status} {Body}",
                    kind,
                    (int)response.StatusCode,
                    body);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return GiphyResponseParser.ExtractHits(doc.RootElement);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Giphy {Kind} request errored for query '{Query}'.", kind, query);
            return [];
        }
    }
}

public static class GiphyResponseParser
{
    public static string[] ExtractGifUrls(JsonElement root) =>
        ExtractHits(root).Select(hit => hit.Url).ToArray();

    public static List<GiphyGifHit> ExtractHits(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data))
        {
            return [];
        }

        if (data.ValueKind == JsonValueKind.Object)
        {
            var hit = ExtractHit(data);
            return hit is null ? [] : [hit];
        }

        if (data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<GiphyGifHit>();
        foreach (var item in data.EnumerateArray())
        {
            var hit = ExtractHit(item);
            if (hit is not null)
            {
                found.Add(hit);
            }
        }

        return found;
    }

    private static GiphyGifHit? ExtractHit(JsonElement item)
    {
        if (!item.TryGetProperty("images", out var images))
        {
            return null;
        }

        var gifUrl = ExtractImageUrl(images, "original")
            ?? ExtractImageUrl(images, "fixed_height")
            ?? ExtractImageUrl(images, "downsized");
        if (string.IsNullOrWhiteSpace(gifUrl))
        {
            return null;
        }

        var jsonId = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        return new GiphyGifHit(GiphyGifSelector.Identity(jsonId, gifUrl), gifUrl);
    }

    private static string? ExtractImageUrl(JsonElement images, string formatName) =>
        images.TryGetProperty(formatName, out var format) &&
        format.TryGetProperty("url", out var urlEl)
            ? urlEl.GetString()
            : null;
}
