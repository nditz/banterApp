using System.Text.Json;

namespace BanterApp.Api.Integrations.Rss;

public static class ApplePodcastLookup
{
    public static string LookupUrl(long applePodcastId) =>
        $"https://itunes.apple.com/lookup?id={applePodcastId}";

    public static string? ParseFeedUrl(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Array ||
                results.GetArrayLength() == 0)
            {
                return null;
            }

            var first = results[0];
            if (!first.TryGetProperty("feedUrl", out var feedUrl) ||
                feedUrl.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = feedUrl.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
