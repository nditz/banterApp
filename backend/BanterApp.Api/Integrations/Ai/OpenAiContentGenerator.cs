using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Ai;

/// <summary>
/// OpenAI ChatGPT + DALL-E provider for feed reactions, banter text, and meme-style images.
/// </summary>
public sealed class OpenAiContentGenerator : IContentGenerator
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiContentGenerator> _logger;
    private readonly ConcurrentDictionary<string, int> _generationCounts = new();

    public OpenAiContentGenerator(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<OpenAiContentGenerator> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> CanGenerateAsync(
        string? userId,
        bool isAnonymous,
        CancellationToken cancellationToken = default)
    {
        if (!isAnonymous)
        {
            return Task.FromResult(true);
        }

        var key = ResolveUserKey(userId, isAnonymous);
        var count = _generationCounts.GetValueOrDefault(key, 0);
        return Task.FromResult(count < _options.AnonymousGenerationLimit);
    }

    public async Task<string> GenerateBanterAsync(
        string userPrediction,
        string actualResult,
        BanterTone tone = BanterTone.Friendly,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);

        var systemPrompt =
            "You are a witty football fan on a banter app. Write 1-2 short sentences roasting or praising " +
            "a prediction vs result. PG-rated, no gambling. Tone: " + tone.ToString().ToLowerInvariant() + ".";

        var userPrompt = $"Prediction: {userPrediction}\nActual result: {actualResult}";
        return await CompleteChatAsync(systemPrompt, userPrompt, cancellationToken);
    }

    public async Task<string> GenerateAnalysisAsync(
        string userPrediction,
        MatchStatisticsDto matchStats,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);

        var systemPrompt =
            "You are a sharp football analyst on a banter app. Explain in 2-3 sentences how the stats " +
            "support or undermine the user's prediction. Reference possession, shots, and key numbers. PG-rated.";

        var userPrompt =
            $"User prediction: {userPrediction}\n" +
            $"Possession: {matchStats.HomePossessionPercent}% vs {matchStats.AwayPossessionPercent}%\n" +
            $"Shots: {matchStats.HomeShots}-{matchStats.AwayShots}, on target {matchStats.HomeShotsOnTarget}-{matchStats.AwayShotsOnTarget}\n" +
            $"Corners: {matchStats.HomeCorners}-{matchStats.AwayCorners}";

        return await CompleteChatAsync(systemPrompt, userPrompt, cancellationToken);
    }

    public async Task<string> GenerateMemeCaptionAsync(
        string context,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);

        var systemPrompt =
            "Write a viral football meme caption (one line, POV / reaction meme style). PG-rated, no slurs.";
        return await CompleteChatAsync(systemPrompt, context, cancellationToken);
    }

    public async Task<string> GenerateVideoScriptAsync(
        VideoScriptFormat format,
        VideoScriptDuration duration,
        string context,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);

        var systemPrompt =
            $"Write a {duration} second {format} video script with timestamps. Hook in first 3 seconds. CTA at end.";
        return await CompleteChatAsync(systemPrompt, context, cancellationToken);
    }

    public async Task<string> GenerateNewsReactionAsync(
        string headline,
        string summary,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var userPrompt = string.IsNullOrWhiteSpace(category)
            ? $"Headline: {headline}\nSummary: {summary}"
            : $"Category: {category}\nHeadline: {headline}\nSummary: {summary}";

        return await CompleteChatAsync(_options.NewsReactionSystemPrompt, userPrompt, cancellationToken);
    }

    public async Task<string?> GenerateReactionImageUrlAsync(
        string headline,
        string reactionText,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableImageGeneration || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return null;
        }

        var scenePrompt =
            $"{_options.MemeImagePrompt}\nSubject: {headline}\nBanter: {reactionText}";
        if (!string.IsNullOrWhiteSpace(category))
        {
            scenePrompt += $"\nContext: {category}";
        }

        return await GenerateImageAsync(scenePrompt, cancellationToken);
    }

    public async Task<FeedVisualSuggestion> SuggestFeedVisualAsync(
        string headline,
        string reactionText,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return StubVisualFromSeed(headline, reactionText, category);
        }

        var userPrompt =
            $"Headline: {headline}\nReaction: {reactionText}\n" +
            (string.IsNullOrWhiteSpace(category) ? "" : $"Category: {category}\n") +
            "Pick the best visual for a football banter feed card.";

        try
        {
            var json = await CompleteChatAsync(
                _options.FeedVisualSystemPrompt,
                userPrompt,
                cancellationToken,
                responseFormatJson: true);

            return ParseVisualSuggestion(json) ?? StubVisualFromSeed(headline, reactionText, category);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feed visual suggestion failed; using deterministic fallback.");
            return StubVisualFromSeed(headline, reactionText, category);
        }
    }

    private static FeedVisualSuggestion StubVisualFromSeed(
        string headline,
        string reactionText,
        string? category)
    {
        var seed = $"{headline}|{reactionText}|{category}";
        var moods = new[] { "celebrate", "debate", "shock", "facepalm", "hype", "pundit" };
        var mood = moods[Math.Abs(seed.GetHashCode()) % moods.Length];
        var useGif = Math.Abs(seed.GetHashCode()) % 3 != 0;
        return useGif
            ? new FeedVisualSuggestion("gif", mood, null)
            : new FeedVisualSuggestion("image", null, $"Football banter scene: {headline}");
    }

    private static FeedVisualSuggestion? ParseVisualSuggestion(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var format = root.TryGetProperty("format", out var f) ? f.GetString() : "gif";
            var mood = root.TryGetProperty("mood", out var m) ? m.GetString() : null;
            var prompt = root.TryGetProperty("imagePrompt", out var p) ? p.GetString() : null;

            if (string.IsNullOrWhiteSpace(format))
            {
                return null;
            }

            return new FeedVisualSuggestion(format.Trim().ToLowerInvariant(), mood, prompt);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> CompleteChatAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        bool responseFormatJson = false)
    {
        var baseUrl = ResolveBaseUrl();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = _options.MaxTokens,
            temperature = _options.Temperature,
            response_format = responseFormatJson ? new { type = "json_object" } : null
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "OpenAI chat request failed: {Status} {Body}",
                (int)response.StatusCode,
                errorBody);
            throw new InvalidOperationException($"OpenAI chat request failed ({(int)response.StatusCode}).");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return string.IsNullOrWhiteSpace(content) ? string.Empty : content.Trim();
    }

    private async Task<string?> GenerateImageAsync(string prompt, CancellationToken cancellationToken)
    {
        var baseUrl = ResolveBaseUrl();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/images/generations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _options.ImageModel,
            prompt,
            n = 1,
            size = _options.ImageSize
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "OpenAI image request failed: {Status} {Body}",
                (int)response.StatusCode,
                errorBody);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Array ||
            data.GetArrayLength() == 0)
        {
            return null;
        }

        var url = data[0].TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;
        return string.IsNullOrWhiteSpace(url) ? null : url;
    }

    private string ResolveBaseUrl() =>
        string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.openai.com/v1"
            : _options.BaseUrl.TrimEnd('/');

    private async Task EnsureCanGenerateAsync(
        string? userId,
        bool isAnonymous,
        CancellationToken cancellationToken)
    {
        if (!await CanGenerateAsync(userId, isAnonymous, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Anonymous users are limited to {_options.AnonymousGenerationLimit} AI content generations.");
        }

        if (isAnonymous)
        {
            var key = ResolveUserKey(userId, isAnonymous);
            _generationCounts.AddOrUpdate(key, 1, static (_, current) => current + 1);
        }
    }

    private static string ResolveUserKey(string? userId, bool isAnonymous)
    {
        if (!isAnonymous)
        {
            return userId ?? "registered-anonymous-fallback";
        }

        return string.IsNullOrWhiteSpace(userId) ? "anonymous-guest" : $"anonymous:{userId}";
    }
}
