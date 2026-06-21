using System.Text.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media.Dtos;
using BanterApp.Api.Services;
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

    Task<IReadOnlyList<MediaItemDto>> SearchVideosAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaItemDto>> GetVideoMetadataAsync(
        IReadOnlyList<string> videoIds,
        CancellationToken cancellationToken = default);
}

public sealed class YouTubeProvider : IYouTubeProvider
{
    private readonly HttpClient _httpClient;
    private readonly YouTubeOptions _options;
    private readonly ILogger<YouTubeProvider> _logger;
    private readonly IErrorTrackingService _errorTracking;

    public YouTubeProvider(
        HttpClient httpClient,
        IOptions<YouTubeOptions> options,
        ILogger<YouTubeProvider> logger,
        IErrorTrackingService errorTracking)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _errorTracking = errorTracking;
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
                $"{_options.BaseUrl.TrimEnd('/')}/search?part=snippet&channelId={Uri.EscapeDataString(channelId)}" +
                "&q=World+Cup+prediction&type=video&order=date&maxResults=" +
                $"{Math.Clamp(maxResults, 1, 25)}&key={_options.ApiKey}";

            using var searchResponse = await _httpClient.GetAsync(searchUrl, cancellationToken);
            if (!searchResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("YouTube search failed for channel {ChannelId}: {Status}",
                    channelId, (int)searchResponse.StatusCode);
                await TrackYouTubeErrorAsync((int)searchResponse.StatusCode, "search", channelId: channelId, ct: cancellationToken);
                return [];
            }

            await using var stream = await searchResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return MapSearchResults(document.RootElement, channelId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YouTube channel fetch failed for {ChannelId}.", channelId);
            await TrackYouTubeExceptionAsync(ex, "search", channelId: channelId, ct: cancellationToken);
            return [];
        }
    }

    public async Task<IReadOnlyList<MediaItemDto>> SearchVideosAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        try
        {
            var searchUrl =
                $"{_options.BaseUrl.TrimEnd('/')}/search?part=snippet" +
                $"&q={Uri.EscapeDataString(query)}" +
                "&type=video&order=date&maxResults=" +
                $"{Math.Clamp(maxResults, 1, 25)}&key={_options.ApiKey}";

            using var searchResponse = await _httpClient.GetAsync(searchUrl, cancellationToken);
            if (!searchResponse.IsSuccessStatusCode)
            {
                if ((int)searchResponse.StatusCode is 403 or 429)
                {
                    _logger.LogWarning("YouTube quota/rate limit for query {Query}: {Status}", query, (int)searchResponse.StatusCode);
                }
                else
                {
                    _logger.LogWarning("YouTube search failed for query {Query}: {Status}", query, (int)searchResponse.StatusCode);
                }

                return [];
            }

            await using var stream = await searchResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return MapSearchResults(document.RootElement, query);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YouTube search failed for query {Query}.", query);
            return [];
        }
    }

    public async Task<IReadOnlyList<MediaItemDto>> GetVideoMetadataAsync(
        IReadOnlyList<string> videoIds,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || videoIds.Count == 0)
        {
            return [];
        }

        var ids = string.Join(',', videoIds.Take(50));
        try
        {
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/videos?part=snippet,contentDetails&id={ids}&key={_options.ApiKey}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("YouTube videos metadata failed: {Status}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var results = new List<MediaItemDto>();
            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var idEl))
                {
                    continue;
                }

                var videoId = idEl.GetString() ?? string.Empty;
                if (!item.TryGetProperty("snippet", out var snippet))
                {
                    continue;
                }

                var channelTitle = snippet.TryGetProperty("channelTitle", out var chEl)
                    ? chEl.GetString()
                    : null;
                results.Add(MapSnippet(videoId, snippet, channelTitle ?? videoId));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YouTube metadata fetch failed.");
            return [];
        }
    }

    private static IReadOnlyList<MediaItemDto> MapSearchResults(JsonElement root, string sourceExternalId)
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
            var channelTitle = snippet.TryGetProperty("channelTitle", out var chEl) ? chEl.GetString() : null;
            results.Add(MapSnippet(videoId, snippet, sourceExternalId, channelTitle));
        }

        return results;
    }

    private static MediaItemDto MapSnippet(
        string videoId,
        JsonElement snippet,
        string sourceExternalId,
        string? channelTitle = null)
    {
        var title = snippet.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty;
        var description = snippet.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;
        DateTimeOffset? publishedAt = null;
        if (snippet.TryGetProperty("publishedAt", out var pubEl) &&
            DateTimeOffset.TryParse(pubEl.GetString(), out var parsed))
        {
            publishedAt = parsed;
        }

        var channel = channelTitle
            ?? (snippet.TryGetProperty("channelTitle", out var chEl) ? chEl.GetString() : null);

        return new MediaItemDto(
            videoId,
            title,
            description,
            $"https://www.youtube.com/watch?v={videoId}",
            null,
            publishedAt,
            sourceExternalId,
            Author: channel,
            Publication: channel,
            FullText: description);
    }

    private Task TrackYouTubeErrorAsync(int statusCode, string operation, string? channelId = null, string? videoId = null, CancellationToken ct = default)
    {
        var mapped = ProviderErrorMapper.MapYouTube(statusCode, operation, videoId, channelId);
        return _errorTracking.TrackAsync(new ErrorTrackRequest
        {
            Source = "provider",
            ErrorCode = mapped.Code,
            MessageSafe = mapped.SafeMessage,
            Severity = "error",
            Provider = "youtube",
            IsRetryable = mapped.IsRetryable,
            Metadata = mapped.Metadata
        }, ct);
    }

    private Task TrackYouTubeExceptionAsync(Exception ex, string operation, string? channelId = null, string? videoId = null, CancellationToken ct = default)
    {
        return _errorTracking.TrackExceptionAsync(new ErrorTrackRequest
        {
            Source = "provider",
            ErrorCode = ErrorCodes.YouTubeApiError,
            MessageSafe = "We could not load this video right now.",
            Severity = "error",
            Provider = "youtube",
            IsRetryable = true,
            Metadata = new Dictionary<string, object?>
            {
                ["operation"] = operation,
                ["channel_id"] = channelId,
                ["video_id"] = videoId
            }
        }, ex, ct);
    }
}
