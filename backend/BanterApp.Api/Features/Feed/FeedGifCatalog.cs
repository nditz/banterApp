namespace BanterApp.Api.Features.Feed;

/// <summary>Curated football-reaction GIFs keyed by ChatGPT mood tags.</summary>
public static class FeedGifCatalog
{
    private static readonly Dictionary<string, string> MoodToUrl = new(StringComparer.OrdinalIgnoreCase)
    {
        ["celebrate"] = "https://media.giphy.com/media/26gsjCZpPolPr3sBy/giphy.gif",
        ["win"] = "https://media.giphy.com/media/26gsjCZpPolPr3sBy/giphy.gif",
        ["hype"] = "https://media.giphy.com/media/l0HlBO7eyXzSZkJri/giphy.gif",
        ["debate"] = "https://media.giphy.com/media/3o7TKSjRrfIPjeiVy/giphy.gif",
        ["shock"] = "https://media.giphy.com/media/3o6Zt481isNVkbQIhr/giphy.gif",
        ["chaos"] = "https://media.giphy.com/media/3o6Zt481isNVkbQIhr/giphy.gif",
        ["facepalm"] = "https://media.giphy.com/media/ISOckXU5oKAE/giphy.gif",
        ["miss"] = "https://media.giphy.com/media/ISOckXU5oKAE/giphy.gif",
        ["roast"] = "https://media.giphy.com/media/3o6Zt8rCfNXzYvNj2E/giphy.gif",
        ["trophy"] = "https://media.giphy.com/media/3o6Zt6MLCHB0UiZ48I/giphy.gif",
        ["news"] = "https://media.giphy.com/media/26BRuo6sGiljlMz4s/giphy.gif",
        ["pundit"] = "https://media.giphy.com/media/3o7aD2saQq3B5iyTFS/giphy.gif",
        ["cooked"] = "https://media.giphy.com/media/l0HlBO7eyXzSZkJri/giphy.gif",
        ["ratio"] = "https://media.giphy.com/media/3o6Zt8rCfNXzYvNj2E/giphy.gif",
        ["delulu"] = "https://media.giphy.com/media/3o7TKSjRrfIPjeiVy/giphy.gif",
        ["maincharacter"] = "https://media.giphy.com/media/l0HlBO7eyXzSZkJri/giphy.gif",
    };

    public static string ResolveGifUrl(string? mood, string fallbackMood = "news")
    {
        if (!string.IsNullOrWhiteSpace(mood) && MoodToUrl.TryGetValue(mood.Trim(), out var url))
        {
            return url;
        }

        return MoodToUrl.GetValueOrDefault(fallbackMood, MoodToUrl["news"]);
    }

    public static IReadOnlyCollection<string> ValidMoods => MoodToUrl.Keys;
}
