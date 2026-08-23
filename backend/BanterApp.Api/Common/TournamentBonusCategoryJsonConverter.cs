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
                : throw new JsonException($"Unknown season award category: {numeric}");
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Season award category must be a string or number.");
        }

        return reader.GetString() switch
        {
            "player_of_the_season" or "PlayerOfTheSeason" or "player_of_tournament" or "PlayerOfTournament"
                => TournamentBonusCategory.PlayerOfTheSeason,
            "golden_boot" or "GoldenBoot" or "top_scorer" or "TopScorer"
                => TournamentBonusCategory.GoldenBoot,
            "most_assists" or "MostAssists" or "top_assist" or "TopAssist"
                => TournamentBonusCategory.MostAssists,
            "golden_glove" or "GoldenGlove" => TournamentBonusCategory.GoldenGlove,
            "surprise_team" or "SurpriseTeam" or "surprise_package" or "SurprisePackage"
                => TournamentBonusCategory.SurpriseTeam,
            "league_winner" or "LeagueWinner" => TournamentBonusCategory.LeagueWinner,
            "top_four" or "TopFour" => TournamentBonusCategory.TopFour,
            "relegated" or "Relegated" => TournamentBonusCategory.Relegated,
            "young_player_of_the_season" or "YoungPlayerOfTheSeason" or "best_young_player"
                => TournamentBonusCategory.YoungPlayerOfTheSeason,
            null => throw new JsonException("Season award category is required."),
            var value => throw new JsonException($"Unknown season award category: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, TournamentBonusCategory value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ToApiString(value));

    public static string ToApiString(TournamentBonusCategory value) =>
        value switch
        {
            TournamentBonusCategory.GoldenBoot => "golden_boot",
            TournamentBonusCategory.MostAssists => "most_assists",
            TournamentBonusCategory.GoldenGlove => "golden_glove",
            TournamentBonusCategory.SurpriseTeam => "surprise_team",
            TournamentBonusCategory.LeagueWinner => "league_winner",
            TournamentBonusCategory.TopFour => "top_four",
            TournamentBonusCategory.Relegated => "relegated",
            TournamentBonusCategory.YoungPlayerOfTheSeason => "young_player_of_the_season",
            _ => "player_of_the_season"
        };
}
