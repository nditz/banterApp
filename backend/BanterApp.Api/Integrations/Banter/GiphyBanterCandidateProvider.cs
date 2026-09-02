using System.Text.Json;
using BanterApp.Api.Integrations.Media;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Banter;

/// <summary>
/// Fetches multiple Giphy search hits per concept without claiming ledger slots (pool building).
/// </summary>
public sealed class GiphyBanterCandidateProvider : IBanterCandidateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ReactionGifOptions _gifOptions;
    private readonly ILogger<GiphyBanterCandidateProvider> _logger;

    public GiphyBanterCandidateProvider(
        HttpClient httpClient,
        IOptions<ReactionGifOptions> gifOptions,
        ILogger<GiphyBanterCandidateProvider> logger)
    {
        _httpClient = httpClient;
        _gifOptions = gifOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BanterCandidate>> GetCandidatesAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (!_gifOptions.IsGiphyEnabled || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var pageSize = Math.Clamp(limit, 1, 50);
        var normalized = GiphyGifSelector.TruncateQuery(query);
        var rating = string.IsNullOrWhiteSpace(_gifOptions.ContentRating) ? "pg" : _gifOptions.ContentRating;
        var url =
            $"{_gifOptions.GiphyBaseUrl.TrimEnd('/')}/gifs/search" +
            $"?api_key={Uri.EscapeDataString(_gifOptions.ApiKey!)}" +
            $"&q={Uri.EscapeDataString(normalized)}" +
            $"&limit={pageSize}" +
            $"&offset=0" +
            $"&rating={Uri.EscapeDataString(rating)}" +
            "&lang=en";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "BanterCandidatesFetched provider=giphy success=false status={Status} body={Body}",
                    (int)response.StatusCode,
                    body);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var hits = GiphyResponseParser.ExtractHits(doc.RootElement);
            var candidates = new List<BanterCandidate>(hits.Count);
            for (var i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                candidates.Add(new BanterCandidate(
                    Provider: "giphy",
                    ProviderContentId: hit.Id,
                    ContentType: BanterContentType.Gif,
                    SourceQuery: normalized,
                    Url: hit.Url,
                    ProviderRank: i,
                    Tags: [normalized]));
            }

            _logger.LogInformation(
                "BanterCandidatesFetched provider=giphy query={Query} count={Count}",
                normalized,
                candidates.Count);
            return candidates;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Giphy candidate pool fetch failed for '{Query}'.", normalized);
            return [];
        }
    }
}
