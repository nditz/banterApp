using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BanterApp.Api.Integrations.Ai;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Banter;

/// <summary>
/// Asks GPT for multiple reaction/search concepts as JSON; falls back to predefined phrases.
/// </summary>
public sealed class OpenAiBanterConceptGenerator : IBanterConceptGenerator
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _aiOptions;
    private readonly BanterOptions _banterOptions;
    private readonly ILogger<OpenAiBanterConceptGenerator> _logger;

    public OpenAiBanterConceptGenerator(
        HttpClient httpClient,
        IOptions<AiOptions> aiOptions,
        IOptions<BanterOptions> banterOptions,
        ILogger<OpenAiBanterConceptGenerator> logger)
    {
        _httpClient = httpClient;
        _aiOptions = aiOptions.Value;
        _banterOptions = banterOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BanterConcept>> GenerateAsync(
        BanterContext context,
        BanterScenario scenario,
        BanterExclusionContext exclusions,
        CancellationToken cancellationToken = default)
    {
        var targetCount = _banterOptions.ConceptCount;

        if (string.IsNullOrWhiteSpace(_aiOptions.ApiKey))
        {
            _logger.LogInformation(
                "BanterConceptsGenerated source=predefined reason=no_api_key scenario={Scenario} count={Count}",
                scenario,
                targetCount);
            return Normalize(PredefinedBanterConcepts.ForScenario(scenario, targetCount), exclusions, targetCount, scenario);
        }

        try
        {
            var json = await CompleteConceptsJsonAsync(context, scenario, exclusions, cancellationToken);
            var parsed = ParseConcepts(json);
            var normalized = Normalize(parsed, exclusions, targetCount, scenario);

            _logger.LogInformation(
                "BanterConceptsGenerated source=openai scenario={Scenario} raw={Raw} final={Final}",
                scenario,
                parsed.Count,
                normalized.Count);

            return normalized;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Banter concept GPT failure; using predefined fallbacks for {Scenario}.", scenario);
            var fallback = Normalize(
                PredefinedBanterConcepts.ForScenario(scenario, targetCount),
                exclusions,
                targetCount,
                scenario);
            _logger.LogInformation(
                "BanterFallbackUsed stage=concepts reason=openai_failure scenario={Scenario} count={Count}",
                scenario,
                fallback.Count);
            return fallback;
        }
    }

    public static IReadOnlyList<BanterConcept> ParseConcepts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("concepts", out var concepts) ||
                concepts.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var list = new List<BanterConcept>();
            foreach (var item in concepts.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var phrase = item.GetString();
                    if (!string.IsNullOrWhiteSpace(phrase))
                    {
                        list.Add(new BanterConcept(phrase.Trim()));
                    }
                }
                else if (item.ValueKind == JsonValueKind.Object &&
                         item.TryGetProperty("phrase", out var phraseEl))
                {
                    var phrase = phraseEl.GetString();
                    if (!string.IsNullOrWhiteSpace(phrase))
                    {
                        var tone = item.TryGetProperty("tone", out var toneEl) ? toneEl.GetString() : null;
                        list.Add(new BanterConcept(phrase.Trim(), tone));
                    }
                }
            }

            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<BanterConcept> Normalize(
        IEnumerable<BanterConcept> concepts,
        BanterExclusionContext exclusions,
        int targetCount,
        BanterScenario scenario)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<BanterConcept>();

        foreach (var concept in concepts.Concat(PredefinedBanterConcepts.ForScenario(scenario, targetCount * 2)))
        {
            var cleaned = Clean(concept.Phrase);
            if (cleaned is null)
            {
                continue;
            }

            var key = BanterExclusionContext.NormalizePhrase(cleaned);
            if (exclusions.IsSearchPhraseExcluded(key) || !seen.Add(key))
            {
                continue;
            }

            result.Add(concept with { Phrase = cleaned });
            if (result.Count >= targetCount)
            {
                break;
            }
        }

        return result;
    }

    private static string? Clean(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return null;
        }

        var trimmed = phrase.Trim();
        if (trimmed.Length is < 2 or > 80)
        {
            return null;
        }

        return trimmed;
    }

    private async Task<string> CompleteConceptsJsonAsync(
        BanterContext context,
        BanterScenario scenario,
        BanterExclusionContext exclusions,
        CancellationToken cancellationToken)
    {
        var avoid = exclusions.SearchPhrases.Take(20).ToArray();
        var system =
            "You generate short Giphy search concepts for football banter reactions. " +
            "Return JSON only: {\"scenario\":\"...\",\"concepts\":[\"...\"]}. " +
            "Concepts must be emotional/reaction phrases, not team names. " +
            $"Produce about {_banterOptions.ConceptCount} diverse concepts.";

        var user = new StringBuilder();
        user.AppendLine($"Scenario: {scenario}");
        user.AppendLine($"Headline: {context.Headline}");
        user.AppendLine($"Summary: {context.Summary}");
        user.AppendLine($"Predicted: {context.PredictedOutcome}; Actual: {context.ActualOutcome}");
        user.AppendLine($"Score: {context.HomeScore}-{context.AwayScore}");
        if (avoid.Length > 0)
        {
            user.AppendLine("Avoid these recent phrases:");
            foreach (var phrase in avoid)
            {
                user.AppendLine($"- {phrase}");
            }
        }

        var baseUrl = string.IsNullOrWhiteSpace(_aiOptions.BaseUrl)
            ? "https://api.openai.com/v1"
            : _aiOptions.BaseUrl.TrimEnd('/');

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _aiOptions.ApiKey);
        request.Content = JsonContent.Create(new Dictionary<string, object?>
        {
            ["model"] = _aiOptions.Model,
            ["messages"] = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user.ToString() }
            },
            ["max_completion_tokens"] = Math.Min(800, _aiOptions.MaxTokens),
            ["response_format"] = new { type = "json_object" },
            ["temperature"] = 0.9
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"OpenAI concept request failed ({(int)response.StatusCode}): {body}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}
