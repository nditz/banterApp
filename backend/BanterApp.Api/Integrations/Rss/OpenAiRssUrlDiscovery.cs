using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Services;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Rss;

public sealed class OpenAiRssUrlDiscovery(
    HttpClient httpClient,
    IOptions<AiOptions> options,
    IProviderUsageGuard usage,
    ILogger<OpenAiRssUrlDiscovery> logger) : IRssUrlDiscovery
{
    public const string OpenAiProvider = "openai";

    private readonly AiOptions _options = options.Value;

    public async Task<string?> SuggestFeedUrlAsync(
        RssFeed feed,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return null;
        }

        if (!await usage.CanInvokeAsync(OpenAiProvider, ct: cancellationToken))
        {
            logger.LogInformation("Skipping RSS URL discovery for {Slug}; OpenAI usage guard blocked the call.", feed.Slug);
            return null;
        }

        var started = DateTime.UtcNow;
        try
        {
            var json = await CompleteChatAsync(BuildUserPrompt(feed, failureReason), cancellationToken);
            await usage.RecordSuccessAsync(
                OpenAiProvider,
                latencyMs: (int)(DateTime.UtcNow - started).TotalMilliseconds,
                ct: cancellationToken);
            var url = ParseRssUrl(json);
            if (url is null)
            {
                logger.LogInformation("OpenAI did not suggest an RSS URL for {Slug}.", feed.Slug);
            }

            return url;
        }
        catch (Exception ex)
        {
            await usage.RecordFailureAsync(OpenAiProvider, ex.Message, cancellationToken);
            logger.LogWarning(ex, "OpenAI RSS URL discovery failed for {Slug}.", feed.Slug);
            return null;
        }
    }

    public static string? ParseRssUrl(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var trimmed = json.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                trimmed = trimmed[start..(end + 1)];
            }
        }

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.TryGetProperty("rss_url", out var urlEl))
            {
                if (urlEl.ValueKind == JsonValueKind.Null)
                {
                    return null;
                }

                if (urlEl.ValueKind == JsonValueKind.String)
                {
                    var value = urlEl.GetString()?.Trim();
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }
            }
        }
        catch (JsonException)
        {
            // Fall through to a bare-URL scrape of the model output.
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri.ToString();
        }

        return null;
    }

    private static string BuildUserPrompt(RssFeed feed, string failureReason) =>
        PromptGuard.UntrustedSourceInstruction + "\n\n" +
        "Find a working public RSS or Atom URL for this football source.\n" +
        PromptGuard.WrapUntrustedSource(
            $"name: {feed.Name}\n" +
            $"kind: {feed.Kind}\n" +
            $"site_url: {feed.SiteUrl ?? ""}\n" +
            $"current_rss_url: {feed.RssUrl}\n" +
            $"apple_podcast_id: {feed.ApplePodcastId?.ToString() ?? ""}\n" +
            $"failure_reason: {failureReason}");

    private async Task<string> CompleteChatAsync(string userPrompt, CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.openai.com/v1"
            : _options.BaseUrl.TrimEnd('/');

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["messages"] = new object[]
            {
                new { role = "system", content = _options.RssUrlDiscoverySystemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["max_completion_tokens"] = _options.RssUrlDiscoveryMaxTokens,
            ["response_format"] = new { type = "json_object" }
        };

        if (IsReasoningModel(_options.Model))
        {
            if (!string.IsNullOrWhiteSpace(_options.ReasoningEffort))
            {
                payload["reasoning_effort"] = _options.ReasoningEffort.Trim().ToLowerInvariant();
            }
        }
        else
        {
            payload["temperature"] = _options.RssUrlDiscoveryTemperature;
        }

        request.Content = JsonContent.Create(payload);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "OpenAI RSS URL discovery failed: {Status} {Body}",
                (int)response.StatusCode,
                errorBody);
            throw ProviderErrorMapper.MapOpenAi(
                (int)response.StatusCode,
                "rss.url.discover",
                _options.Model,
                rawMessage: errorBody);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?.Trim() ?? string.Empty;
    }

    private static bool IsReasoningModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        var name = model.Trim().ToLowerInvariant();
        return name.StartsWith("o1", StringComparison.Ordinal)
               || name.StartsWith("o3", StringComparison.Ordinal)
               || name.StartsWith("o4", StringComparison.Ordinal)
               || name.Contains("gpt-5", StringComparison.Ordinal);
    }
}
