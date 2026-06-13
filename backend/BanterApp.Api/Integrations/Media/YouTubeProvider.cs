using System.Text.Json;
using BanterApp.Api.Integrations.Media.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Media;

public interface IYouTubeProvider
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<MediaItemDto>> GetChannelVideosAsync(
        string channelId,
        int maxResults,
        CancellationToken cancellationToken = default);
}

public sealed class YouTubeProvider : IYouTubeProvider
{
    private readonly HttpClient _httpClient;
    private readonly YouTubeOptions _options;
    private readonly ILogger<YouTubeProvider> _logger;

    public YouTubeProvider(
        HttpClient httpClient,
        IOptions<YouTubeOptions> options,
        ILogger<YouTubeProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<IReadOnlyList<MediaItemDto>> GetChannelVideosAsync(
        string channelId,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        try
        {
            var searchUrl =
                $"{_options.BaseUrl.TrimEnd('/')}/search?part=snippet&channelId={channelId}" +
                "&q=World+Cup+prediction&type=video&order=date&maxResults=" +
                $"{Math.Clamp(maxResults, 1, 25)}&key={_options.ApiKey}";

            using var searchResponse = await _httpClient.GetAsync(searchUrl, cancellationToken);
            if (!searchResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("YouTube search failed for channel {ChannelId}: {Status}",
                    channelId, (int)searchResponse.StatusCode);
                return [];
            }

            await using var stream = await searchResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return MapSearchResults(document.RootElement, channelId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YouTube channel fetch failed for {ChannelId}.", channelId);
            return [];
        }
    }

    private static IReadOnlyList<MediaItemDto> MapSearchResults(JsonElement root, string channelId)
    {
        if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<MediaItemDto>();
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idEl) ||
                !idEl.TryGetProperty("videoId", out var videoIdEl))
            {
                continue;
            }

            if (!item.TryGetProperty("snippet", out var snippet))
            {
                continue;
            }

            var videoId = videoIdEl.GetString() ?? string.Empty;
            var title = snippet.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty;
            var description = snippet.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;
            DateTimeOffset? publishedAt = null;
            if (snippet.TryGetProperty("publishedAt", out var pubEl) &&
                DateTimeOffset.TryParse(pubEl.GetString(), out var parsed))
            {
                publishedAt = parsed;
            }

            results.Add(new MediaItemDto(
                videoId,
                title,
                description,
                $"https://www.youtube.com/watch?v={videoId}",
                null,
                publishedAt,
                channelId));
        }

        return results;
    }
}
