using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Features.Ai;
using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Integrations.FootballBanter;
using BanterApp.Api.Integrations.SportsData.Dtos;
using BanterApp.Api.Services;
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

    public async Task<string> GeneratePunditScriptAsync(
        MatchScriptContext context,
        PunditPersonaSeed persona,
        string phase,
        VideoScriptDuration duration,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);

        var style = PunditStyleProfiles.Get(persona.StyleSlug);
        var systemPrompt = PunditScriptPromptBuilder.BuildSystemPrompt(
            _options.PunditScriptSystemPrompt, persona, style);
        var userPrompt = PunditScriptPromptBuilder.BuildUserPrompt(context, persona, phase, duration);

        return await CompleteChatAsync(
            systemPrompt,
            userPrompt,
            cancellationToken,
            temperature: _options.PunditScriptTemperature,
            maxTokens: _options.PunditScriptMaxTokens);
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

    public async Task<FeedBanterCard> GenerateFeedBanterCardAsync(
        string headline,
        string summary,
        string? category = null,
        string? author = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return StubFeedBanterFromSeed(headline, summary, category, author);
        }

        var userPrompt =
            $"Category: {category ?? "news"}\n" +
            (string.IsNullOrWhiteSpace(author) ? "" : $"Pundit/author: {author}\n") +
            $"Headline: {headline}\nSummary: {summary}";

        try
        {
            var json = await CompleteChatAsync(
                _options.FeedBanterSystemPrompt,
                userPrompt,
                cancellationToken,
                responseFormatJson: true);

            var parsed = ParseFeedBanterCard(json);
            if (parsed is not null)
            {
                return parsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feed banter rewrite failed; using stub fallback.");
        }

        return StubFeedBanterFromSeed(headline, summary, category, author);
    }

    public async Task<string> GenerateUsernameSuggestionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return await new StubContentGenerator().GenerateUsernameSuggestionAsync(cancellationToken);
        }

        const string systemPrompt =
            "Generate cool nicknames like they have in board games like Dungeons and Dragons. " +
            "Return ONLY one nickname: 3-20 characters, letters A-Z and numbers 0-9 only, no spaces or punctuation, PG-rated.";

        try
        {
            var raw = await CompleteChatAsync(
                systemPrompt,
                "Generate one unique fantasy-style nickname for a football predictions league player.",
                cancellationToken,
                maxTokens: 24,
                temperature: 1.0);

            var sanitized = BanterApp.Api.Services.UsernameRules.Sanitize(raw?.Trim());
            if (!string.IsNullOrWhiteSpace(sanitized))
            {
                return sanitized;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Username suggestion via OpenAI failed; using stub fallback.");
        }

        return await new StubContentGenerator().GenerateUsernameSuggestionAsync(cancellationToken);
    }

    private static FeedBanterCard StubFeedBanterFromSeed(
        string headline,
        string summary,
        string? category,
        string? author)
    {
        var moods = new[] { "celebrate", "debate", "shock", "facepalm", "hype", "pundit", "cooked", "ratio" };
        var mood = moods[Math.Abs($"{headline}|{category}".GetHashCode()) % moods.Length];
        var hook = headline.Length > 80 ? headline[..77] + "…" : headline;
        var title = category == "pundit_quote" && !string.IsNullOrWhiteSpace(author)
            ? $"{author} said WHAT now? 💀"
            : $"No cap: {hook}";

        var body = $"Lowkey this is giving chaos energy — {summary.Trim()}";
        if (!string.IsNullOrWhiteSpace(author))
        {
            body += $"\n\n({author} really said that on the record. The group chat is not recovering.)";
        }

        return new FeedBanterCard(title, body, mood, "POV: you read this headline and immediately opened the comments.");
    }

    private static FeedBanterCard? ParseFeedBanterCard(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            var body = root.TryGetProperty("body", out var b) ? b.GetString() : null;
            var mood = root.TryGetProperty("mood", out var m) ? m.GetString() : "news";
            var jokeLine = root.TryGetProperty("jokeLine", out var j) ? j.GetString() : null;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            return new FeedBanterCard(
                title.Trim(),
                body.Trim(),
                string.IsNullOrWhiteSpace(mood) ? "news" : mood.Trim(),
                string.IsNullOrWhiteSpace(jokeLine) ? null : jokeLine.Trim());
        }
        catch
        {
            return null;
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
        return new FeedVisualSuggestion("gif", mood, null);
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
            var gifQuery = root.TryGetProperty("gifQuery", out var q) ? q.GetString() : null;

            if (string.IsNullOrWhiteSpace(format))
            {
                return null;
            }

            return new FeedVisualSuggestion(
                format.Trim().ToLowerInvariant(),
                mood,
                prompt,
                string.IsNullOrWhiteSpace(gifQuery) ? null : gifQuery.Trim());
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
        bool responseFormatJson = false,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null)
    {
        var baseUrl = ResolveBaseUrl();
        var effectiveModel = model ?? _options.Model;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = effectiveModel,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            // Newer models (o-series, gpt-5) require max_completion_tokens and reject max_tokens.
            ["max_completion_tokens"] = maxTokens ?? _options.MaxTokens
        };

        if (responseFormatJson)
        {
            payload["response_format"] = new { type = "json_object" };
        }

        // Reasoning models reject a custom temperature (400) and use reasoning_effort instead.
        if (IsReasoningModel(effectiveModel))
        {
            if (!string.IsNullOrWhiteSpace(_options.ReasoningEffort))
            {
                payload["reasoning_effort"] = _options.ReasoningEffort.Trim().ToLowerInvariant();
            }
        }
        else
        {
            payload["temperature"] = temperature ?? _options.Temperature;
        }

        request.Content = JsonContent.Create(payload);

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

    private static bool IsReasoningModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        var normalized = model.Trim().ToLowerInvariant();
        return normalized.StartsWith("o1", StringComparison.Ordinal)
            || normalized.StartsWith("o3", StringComparison.Ordinal)
            || normalized.StartsWith("o4", StringComparison.Ordinal)
            || normalized.StartsWith("gpt-5", StringComparison.Ordinal);
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

    public async Task<string> GenerateFootballBanterJsonAsync(
        FootballBanterSourceInput input,
        string systemPrompt,
        FootballBanterOpenAiConfig openAiConfig,
        int banterIntensity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return FootballBanterStubOutputBuilder.BuildJson(input);
        }

        var userPrompt = BuildFootballBanterUserPrompt(input, banterIntensity);
        return await CompleteChatAsync(
            systemPrompt,
            userPrompt,
            cancellationToken,
            responseFormatJson: true,
            model: openAiConfig.Model,
            temperature: openAiConfig.Temperature,
            maxTokens: openAiConfig.MaxOutputTokens);
    }

    private static string BuildFootballBanterUserPrompt(FootballBanterSourceInput input, int banterIntensity)
    {
        var payload = new Dictionary<string, object?>
        {
            ["source_type"] = input.SourceType,
            ["source_name"] = input.SourceName,
            ["source_url"] = input.SourceUrl,
            ["source_title"] = input.SourceTitle,
            ["published_at"] = input.PublishedAt?.ToString("O"),
            ["pundit_name"] = input.PunditName,
            ["source_text"] = input.SourceText,
            ["prediction"] = input.Prediction,
            ["confidence"] = input.Confidence,
            ["statement_type"] = input.StatementType is null
                ? null
                : FootballBanterOutputParser.ToJsonString(input.StatementType.Value),
            ["banter_intensity"] = banterIntensity
        };

        if (!string.IsNullOrWhiteSpace(input.ReferenceContextJson))
        {
            payload["reference_context"] = input.ReferenceContextJson;
            payload["instruction"] =
                "Only cite statistics from reference_context; do not invent numbers or player/country stats.";
        }

        return JsonSerializer.Serialize(payload, FootballBanterJson.OutputOptions);
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
