namespace BanterApp.Api.Features.Feed;

/// <summary>
/// Curated football-reaction stickers keyed by ChatGPT mood tags. Each mood maps to a
/// pool of assets so cards with the same mood no longer all share one image.
/// Uses bundled local stickers (served by the frontend from <c>/reactions</c>) instead of
/// external Giphy links, which return 404 once the upstream media IDs are retired.
/// </summary>
public static class FeedGifCatalog
{
    // Local reaction stickers (frontend /public/reactions), reused across mood pools for variety.
    private const string Celebrate = "/reactions/receipts-found.svg";
    private const string Hype = "/reactions/locked-in.svg";
    private const string Debate = "/reactions/against-grain.svg";
    private const string Shock = "/reactions/chaos-pick.svg";
    private const string Facepalm = "/reactions/prediction-fraud.svg";
    private const string Roast = "/reactions/brave-but-wrong.svg";
    private const string Trophy = "/reactions/script-writer.svg";
    private const string News = "/reactions/smart-choice.svg";
    private const string Pundit = "/reactions/playing-safe.svg";

    private const string FallbackMood = "news";

    private static readonly Dictionary<string, string[]> MoodToUrls = new(StringComparer.OrdinalIgnoreCase)
    {
        ["celebrate"] = new[] { Celebrate, Trophy, Hype },
        ["win"] = new[] { Celebrate, Trophy },
        ["hype"] = new[] { Hype, Celebrate, Shock },
        ["debate"] = new[] { Debate, Pundit, Roast },
        ["shock"] = new[] { Shock, Debate },
        ["chaos"] = new[] { Shock, Roast, Hype },
        ["facepalm"] = new[] { Facepalm, Roast },
        ["miss"] = new[] { Facepalm, Roast },
        ["roast"] = new[] { Roast, Facepalm, Debate },
        ["trophy"] = new[] { Trophy, Celebrate },
        ["news"] = new[] { News, Pundit, Debate },
        ["pundit"] = new[] { Pundit, Debate, News, Roast },
        ["cooked"] = new[] { Hype, Facepalm },
        ["ratio"] = new[] { Roast, Debate },
        ["delulu"] = new[] { Debate, Shock },
        ["maincharacter"] = new[] { Hype, Celebrate },
    };

    /// <summary>Picks a random URL from the mood pool (used at write time for variety).</summary>
    public static string ResolveGifUrl(string? mood, string fallbackMood = FallbackMood) =>
        Pick(GetPool(mood, fallbackMood), Random.Shared.Next());

    /// <summary>Picks a stable URL from the mood pool for a given seed (same card -> same GIF).</summary>
    public static string ResolveGifUrl(string? mood, int seed, string fallbackMood = FallbackMood) =>
        Pick(GetPool(mood, fallbackMood), seed);

    /// <summary>
    /// Given a URL already used in the feed, returns a different URL from the same mood
    /// pool that is not in <paramref name="usedUrls"/>. Falls back to the original URL.
    /// </summary>
    public static string ResolveAlternate(string currentUrl, ISet<string> usedUrls)
    {
        foreach (var pool in MoodToUrls.Values)
        {
            if (Array.IndexOf(pool, currentUrl) < 0)
            {
                continue;
            }

            foreach (var candidate in pool)
            {
                if (!usedUrls.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        return currentUrl;
    }

    private static string[] GetPool(string? mood, string fallbackMood)
    {
        if (!string.IsNullOrWhiteSpace(mood) &&
            MoodToUrls.TryGetValue(mood.Trim(), out var urls) &&
            urls.Length > 0)
        {
            return urls;
        }

        if (!string.IsNullOrWhiteSpace(fallbackMood) &&
            MoodToUrls.TryGetValue(fallbackMood.Trim(), out var fallback) &&
            fallback.Length > 0)
        {
            return fallback;
        }

        return MoodToUrls[FallbackMood];
    }

    private static string Pick(string[] pool, int seed)
    {
        if (pool.Length == 1)
        {
            return pool[0];
        }

        var index = (int)((uint)seed % (uint)pool.Length);
        return pool[index];
    }

    public static IReadOnlyCollection<string> ValidMoods => MoodToUrls.Keys;
}
