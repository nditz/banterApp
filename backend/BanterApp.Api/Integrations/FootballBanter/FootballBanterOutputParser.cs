using System.Text.Json;

namespace BanterApp.Api.Integrations.FootballBanter;

public static class FootballBanterOutputParser
{
    public static FootballBanterOutput? TryParse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var output = new FootballBanterOutput
            {
                Headline = ReadString(root, "headline"),
                BanterSummary = ReadString(root, "banter_summary"),
                MemeReactions = ReadStringArray(root, "meme_reactions"),
                GifSuggestions = ReadStringArray(root, "gif_suggestions"),
                FanReactions = ReadStringArray(root, "fan_reactions"),
                Confidence = ReadDouble(root, "confidence"),
                SourceName = ReadString(root, "source_name"),
                SourceUrl = ReadString(root, "source_url"),
                PunditName = ReadOptionalString(root, "pundit_name"),
                Prediction = ReadOptionalString(root, "prediction"),
                StatementType = ParseStatementType(ReadOptionalString(root, "statement_type")),
                NeedsHumanReview = ReadBool(root, "needs_human_review")
            };

            return output;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static FootballBanterStatementType ParseStatementType(string? value) =>
        value?.Trim().ToLowerInvariant().Replace('-', '_') switch
        {
            "direct_quote" => FootballBanterStatementType.DirectQuote,
            "paraphrase" => FootballBanterStatementType.Paraphrase,
            "inferred_prediction" => FootballBanterStatementType.InferredPrediction,
            "ai_summary" or null or "" => FootballBanterStatementType.AiSummary,
            _ => FootballBanterStatementType.AiSummary
        };

    public static string ToJsonString(FootballBanterStatementType type) =>
        type switch
        {
            FootballBanterStatementType.DirectQuote => "direct_quote",
            FootballBanterStatementType.Paraphrase => "paraphrase",
            FootballBanterStatementType.InferredPrediction => "inferred_prediction",
            _ => "ai_summary"
        };

    private static string ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string? ReadOptionalString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double ReadDouble(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String when double.TryParse(value.GetString(), out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool ReadBool(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => false
        };
    }

    private static List<string> ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToList();
    }
}
