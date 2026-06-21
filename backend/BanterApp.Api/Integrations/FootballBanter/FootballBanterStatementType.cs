using System.Text.Json.Serialization;

namespace BanterApp.Api.Integrations.FootballBanter;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FootballBanterStatementType
{
    DirectQuote,
    Paraphrase,
    AiSummary,
    InferredPrediction
}
