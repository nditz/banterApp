using System.Text.Json;
using System.Text.Json.Serialization;
using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Common;

public sealed class TournamentBonusCategoryJsonConverter : JsonConverter<TournamentBonusCategory>
{
    public override TournamentBonusCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numeric))
        {
            return Enum.IsDefined(typeof(TournamentBonusCategory), numeric)
                ? (TournamentBonusCategory)numeric
                : throw new JsonException($"Unknown tournament bonus category: {numeric}");
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Tournament bonus category must be a string or number.");
        }

        return reader.GetString() switch
        {
            "player_of_tournament" or "PlayerOfTournament" => TournamentBonusCategory.PlayerOfTournament,
            "top_scorer" or "TopScorer" => TournamentBonusCategory.TopScorer,
            "top_assist" or "TopAssist" => TournamentBonusCategory.TopAssist,
            "golden_glove" or "GoldenGlove" => TournamentBonusCategory.GoldenGlove,
            "surprise_package" or "SurprisePackage" => TournamentBonusCategory.SurprisePackage,
            null => throw new JsonException("Tournament bonus category is required."),
            var value => throw new JsonException($"Unknown tournament bonus category: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, TournamentBonusCategory value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ToApiString(value));

    public static string ToApiString(TournamentBonusCategory value) =>
        value switch
        {
            TournamentBonusCategory.TopScorer => "top_scorer",
            TournamentBonusCategory.TopAssist => "top_assist",
            TournamentBonusCategory.GoldenGlove => "golden_glove",
            TournamentBonusCategory.SurprisePackage => "surprise_package",
            _ => "player_of_tournament"
        };
}
