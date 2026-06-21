using System.Text.Json;
using System.Text.Json.Serialization;

namespace BanterApp.Api.Integrations.FootballBanter;

internal static class FootballBanterJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public static JsonSerializerOptions OutputOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };
}

internal static class FootballBanterDefaults
{
    public const string EmbeddedSystemPromptFallback =
        "You are the Football Banter Engine. Transform grounded football source content into Gen Z banter JSON. " +
        "Never invent quotes or predictions. Always include source_name and source_url. " +
        "Return JSON only with headline, banter_summary, meme_reactions, gif_suggestions, fan_reactions, " +
        "confidence, source_name, source_url, pundit_name, prediction, statement_type, needs_human_review.";

    public const double ReviewConfidenceThreshold = 0.7;
}
