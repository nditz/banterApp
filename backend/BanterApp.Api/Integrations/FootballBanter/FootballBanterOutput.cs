using System.Text.Json.Serialization;

namespace BanterApp.Api.Integrations.FootballBanter;

public sealed class FootballBanterOutput
{
    [JsonPropertyName("headline")]
    public string Headline { get; set; } = string.Empty;

    [JsonPropertyName("banter_summary")]
    public string BanterSummary { get; set; } = string.Empty;

    [JsonPropertyName("meme_reactions")]
    public List<string> MemeReactions { get; set; } = [];

    [JsonPropertyName("gif_suggestions")]
    public List<string> GifSuggestions { get; set; } = [];

    [JsonPropertyName("fan_reactions")]
    public List<string> FanReactions { get; set; } = [];

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("source_name")]
    public string SourceName { get; set; } = string.Empty;

    [JsonPropertyName("source_url")]
    public string SourceUrl { get; set; } = string.Empty;

    [JsonPropertyName("pundit_name")]
    public string? PunditName { get; set; }

    [JsonPropertyName("prediction")]
    public string? Prediction { get; set; }

    [JsonPropertyName("statement_type")]
    public FootballBanterStatementType StatementType { get; set; } = FootballBanterStatementType.AiSummary;

    [JsonPropertyName("needs_human_review")]
    public bool NeedsHumanReview { get; set; }
}
