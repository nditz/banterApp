using System.Text.Json;
using System.Text.RegularExpressions;
using BanterApp.Api.Integrations.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public interface IYouTubeTranscriptProvider
{
    Task<YouTubeTranscriptResult> GetTranscriptAsync(
        string videoId,
        string? title,
        string? description,
        CancellationToken cancellationToken = default);
}

public sealed record YouTubeTranscriptResult(
    string? TranscriptText,
    string FallbackText,
    bool IsComplete);

public sealed partial class YouTubeTranscriptProvider : IYouTubeTranscriptProvider
{
    private readonly HttpClient _httpClient;
    private readonly YouTubeOptions _options;
    private readonly ILogger<YouTubeTranscriptProvider> _logger;

    public YouTubeTranscriptProvider(
        HttpClient httpClient,
        IOptions<YouTubeOptions> options,
        ILogger<YouTubeTranscriptProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<YouTubeTranscriptResult> GetTranscriptAsync(
        string videoId,
        string? title,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var fallback = BuildFallback(title, description);

        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(videoId))
        {
            return new YouTubeTranscriptResult(null, fallback, IsComplete: false);
        }

        try
        {
            var captionTrack = await GetCaptionTrackAsync(videoId, cancellationToken);
            if (captionTrack is null)
            {
                return new YouTubeTranscriptResult(null, fallback, IsComplete: false);
            }

            var transcript = await FetchTimedTextAsync(videoId, captionTrack, cancellationToken);
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return new YouTubeTranscriptResult(null, fallback, IsComplete: false);
            }

            return new YouTubeTranscriptResult(transcript, fallback, IsComplete: transcript.Length >= 200);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transcript fetch failed for video {VideoId}.", videoId);
            return new YouTubeTranscriptResult(null, fallback, IsComplete: false);
        }
    }

    private async Task<string?> GetCaptionTrackAsync(string videoId, CancellationToken cancellationToken)
    {
        var url =
            $"{_options.BaseUrl.TrimEnd('/')}/captions?part=snippet&videoId={videoId}&key={_options.ApiKey}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!doc.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array ||
            items.GetArrayLength() == 0)
        {
            return null;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("snippet", out var snippet))
            {
                continue;
            }

            var language = snippet.TryGetProperty("language", out var langEl) ? langEl.GetString() : null;
            if (language is "en" or "en-US" or "en-GB")
            {
                return item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            }
        }

        var first = items[0];
        return first.TryGetProperty("id", out var firstId) ? firstId.GetString() : null;
    }

    private async Task<string?> FetchTimedTextAsync(
        string videoId,
        string captionTrackId,
        CancellationToken cancellationToken)
    {
        // Best-effort: timedtext endpoint (may fail without OAuth for some videos).
        var url = $"https://www.youtube.com/api/timedtext?v={videoId}&lang=en";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Timedtext unavailable for {VideoId} caption {CaptionId}.", videoId, captionTrackId);
            return null;
        }

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseTimedText(xml);
    }

    private static string? ParseTimedText(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        var matches = TextTagRegex().Matches(xml);
        if (matches.Count == 0)
        {
            return null;
        }

        var builder = new System.Text.StringBuilder();
        foreach (Match match in matches)
        {
            var text = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                builder.Append(text).Append(' ');
            }
        }

        var result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string BuildFallback(string? title, string? description)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(title))
        {
            parts.Add(title.Trim());
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            parts.Add(description.Trim());
        }

        return string.Join("\n\n", parts);
    }

    [GeneratedRegex("<text[^>]*>([^<]*)</text>")]
    private static partial Regex TextTagRegex();
}
