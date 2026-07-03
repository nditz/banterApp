using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Pundits.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class OpenAiPunditOpinionExtractor : IPunditOpinionExtractor
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly MatchResolutionService _matchResolution;
    private readonly ILogger<OpenAiPunditOpinionExtractor> _logger;

    public OpenAiPunditOpinionExtractor(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        MatchResolutionService matchResolution,
        ILogger<OpenAiPunditOpinionExtractor> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _matchResolution = matchResolution;
        _logger = logger;
    }

    public async Task<PunditExtractionResult?> ExtractAsync(
        string sourceType,
        string sourceName,
        string sourceUrl,
        string sourceTitle,
        DateTimeOffset? publishedAt,
        string? author,
        string sourceText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return null;
        }

        var truncatedText = TruncateForModel(sourceText, 12000);
        var fixtureCatalog = await _matchResolution.BuildFixtureCatalogJsonAsync(cancellationToken: cancellationToken);
        var userPrompt =
            PromptGuard.UntrustedSourceInstruction + "\n\n" +
            "Extract pundit opinions and predictions from this source.\n\n" +
            $"source_type: {sourceType}\n" +
            $"source_name: {sourceName}\n" +
            $"source_url: {sourceUrl}\n" +
            $"source_title: {sourceTitle}\n" +
            $"published_at: {publishedAt:O}\n" +
            $"author: {author ?? "unknown"}\n\n" +
            "Use fixture_catalog ids when a take refers to a specific match.\n" +
            "Return JSON with keys: source_type, source_name, source_url, source_title, published_at, " +
            "pundits (array of {name, role, opinions:[{topic, team, player, match, match_id, opinion, prediction, " +
            "prediction_type, confidence, evidence_quote, quote_context, is_direct_quote, needs_human_review}]}), " +
            "missing_information (array), summary.\n\n" +
            "FIXTURE CATALOG:\n" +
            fixtureCatalog + "\n\n" +
            "SOURCE TEXT:\n" +
            PromptGuard.WrapUntrustedSource(truncatedText);

        try
        {
            var json = await CompleteChatAsync(_options.PunditExtractionSystemPrompt, userPrompt, cancellationToken);
            return ParseExtraction(json, sourceType, sourceName, sourceUrl, sourceTitle, publishedAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pundit extraction failed for {SourceUrl}.", sourceUrl);
            throw;
        }
    }

    private PunditExtractionResult? ParseExtraction(
        string json,
        string sourceType,
        string sourceName,
        string sourceUrl,
        string sourceTitle,
        DateTimeOffset? publishedAt)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var pundits = new List<PunditExtractionPunditDto>();
            if (root.TryGetProperty("pundits", out var punditsEl) && punditsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var punditEl in punditsEl.EnumerateArray())
                {
                    var name = punditEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "Unknown" : "Unknown";
                    var role = punditEl.TryGetProperty("role", out var roleEl) ? roleEl.GetString() ?? "unknown" : "unknown";
                    var opinions = new List<PunditExtractionOpinionDto>();

                    if (punditEl.TryGetProperty("opinions", out var opinionsEl) &&
                        opinionsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var opEl in opinionsEl.EnumerateArray())
                        {
                            try
                            {
                                opinions.Add(new PunditExtractionOpinionDto(
                                    Topic: GetString(opEl, "topic") ?? "World Cup 2026",
                                    Team: GetString(opEl, "team"),
                                    Player: GetString(opEl, "player"),
                                    Match: GetString(opEl, "match"),
                                    MatchId: GetString(opEl, "match_id"),
                                    Opinion: SanitizeField(GetString(opEl, "opinion")) ?? string.Empty,
                                    Prediction: SanitizeField(GetString(opEl, "prediction")),
                                    PredictionType: GetString(opEl, "prediction_type") ?? "unknown",
                                    Confidence: GetDouble(opEl, "confidence") ?? 0.5,
                                    EvidenceQuote: SanitizeField(GetString(opEl, "evidence_quote")),
                                    QuoteContext: SanitizeField(GetString(opEl, "quote_context")),
                                    IsDirectQuote: GetBool(opEl, "is_direct_quote"),
                                    NeedsHumanReview: GetBool(opEl, "needs_human_review") ||
                                                      HtmlSanitizer.ContainsDangerousMarkup(GetString(opEl, "opinion"))));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Skipping malformed opinion entry in extraction JSON.");
                            }
                        }
                    }

                    if (opinions.Count > 0)
                    {
                        pundits.Add(new PunditExtractionPunditDto(name, role, opinions));
                    }
                }
            }

            var missing = new List<string>();
            if (root.TryGetProperty("missing_information", out var missingEl) &&
                missingEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in missingEl.EnumerateArray())
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        missing.Add(value);
                    }
                }
            }

            var summary = GetString(root, "summary") ?? string.Empty;

            return new PunditExtractionResult(
                GetString(root, "source_type") ?? sourceType,
                GetString(root, "source_name") ?? sourceName,
                GetString(root, "source_url") ?? sourceUrl,
                GetString(root, "source_title") ?? sourceTitle,
                publishedAt,
                pundits,
                missing,
                summary,
                json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Malformed pundit extraction JSON.");
            return null;
        }
    }

    private async Task<string> CompleteChatAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
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
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            // Newer models (o-series, gpt-5) require max_completion_tokens and reject max_tokens.
            ["max_completion_tokens"] = _options.PunditExtractionMaxTokens,
            ["response_format"] = new { type = "json_object" }
        };

        // Reasoning models reject a custom temperature (400) and use reasoning_effort instead.
        if (IsReasoningModel(_options.Model))
        {
            if (!string.IsNullOrWhiteSpace(_options.ReasoningEffort))
            {
                payload["reasoning_effort"] = _options.ReasoningEffort.Trim().ToLowerInvariant();
            }
        }
        else
        {
            payload["temperature"] = _options.PunditExtractionTemperature;
        }

        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "OpenAI pundit extraction failed: {Status} {Body}",
                (int)response.StatusCode,
                errorBody);
            throw new InvalidOperationException($"OpenAI pundit extraction failed ({(int)response.StatusCode}).");
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

        var normalized = model.Trim().ToLowerInvariant();
        return normalized.StartsWith("o1", StringComparison.Ordinal)
            || normalized.StartsWith("o3", StringComparison.Ordinal)
            || normalized.StartsWith("o4", StringComparison.Ordinal)
            || normalized.StartsWith("gpt-5", StringComparison.Ordinal);
    }

    private static string? SanitizeField(string? value) =>
        string.IsNullOrWhiteSpace(value) ? value : HtmlSanitizer.SanitizePlainText(value);

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetString() : null;

    private static double? GetDouble(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var parsed))
        {
            return parsed;
        }

        if (value.ValueKind == JsonValueKind.String &&
            double.TryParse(value.GetString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var fromString))
        {
            return fromString;
        }

        return null;
    }

    private static bool GetBool(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False &&
        value.GetBoolean();

    private static string TruncateForModel(string text, int maxChars)
    {
        if (text.Length <= maxChars)
        {
            return text;
        }

        return text[..maxChars] + "\n\n[TRUNCATED]";
    }
}
