using System.Text.Json;
using System.Text.Json.Serialization;
using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Common;

/// <summary>
/// Serializes <see cref="PredictionType"/> as snake_case strings matching the frontend contract.
/// </summary>
public sealed class PredictionTypeJsonConverter : JsonConverter<PredictionType>
{
    public override PredictionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numeric))
        {
            return Enum.IsDefined(typeof(PredictionType), numeric)
                ? (PredictionType)numeric
                : throw new JsonException($"Unknown prediction type value: {numeric}");
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Prediction type must be a string or number.");
        }

        var value = reader.GetString();
        return value switch
        {
            "result" or "Result" => PredictionType.Result,
            "correct_score" or "correctScore" or "CorrectScore" => PredictionType.CorrectScore,
            "double_chance" or "doubleChance" or "DoubleChance" => PredictionType.DoubleChance,
            null => throw new JsonException("Prediction type is required."),
            _ => throw new JsonException($"Unknown prediction type: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, PredictionType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ToApiString(value));
    }

    public static string ToApiString(PredictionType value) =>
        value switch
        {
            PredictionType.CorrectScore => "correct_score",
            PredictionType.DoubleChance => "double_chance",
            _ => "result"
        };
}
