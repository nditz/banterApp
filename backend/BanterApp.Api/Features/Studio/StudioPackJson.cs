using System.Text.Json;
using System.Text.Json.Serialization;

namespace BanterApp.Api.Features.Studio;

public static class StudioPackJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(StudioContentPack pack) =>
        JsonSerializer.Serialize(pack, Options);

    public static StudioContentPack? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StudioContentPack>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
