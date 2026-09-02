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

    /// <summary>True when the URL is a bundled local sticker (not a live Giphy GIF).</summary>
    public static bool IsBundledSticker(string? url) =>
        !string.IsNullOrWhiteSpace(url) &&
        url.StartsWith("/reactions/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Given a URL already used in the feed, returns a different sticker from the full
    /// catalog that is not in <paramref name="usedUrls"/>. Falls back to the original URL.
    /// </summary>
    public static string ResolveAlternate(string currentUrl, ISet<string> usedUrls)
    {
        foreach (var candidate in DistinctUrlsStartingAt(currentUrl))
        {
            if (!usedUrls.Contains(candidate))
            {
                return candidate;
            }
        }

        return currentUrl;
    }

    /// <summary>
    /// Mood pool first, then the rest of the sticker catalog, so memes/stickers can vary
    /// across the whole set instead of repeating 2–3 assets per mood.
    /// </summary>
    public static IEnumerable<string> Candidates(string? mood, int seed)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var url in Rotated(GetPool(mood, FallbackMood), seed))
        {
            if (seen.Add(url))
            {
                yield return url;
            }
        }

        foreach (var url in Rotated(AllDistinctUrls(), seed))
        {
            if (seen.Add(url))
            {
                yield return url;
            }
        }
    }

    public static IReadOnlyList<string> AllDistinctUrls()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var urls = new List<string>();
        foreach (var pool in MoodToUrls.Values)
        {
            foreach (var url in pool)
            {
                if (seen.Add(url))
                {
                    urls.Add(url);
                }
            }
        }

        return urls;
    }

    private static IEnumerable<string> DistinctUrlsStartingAt(string currentUrl)
    {
        var all = AllDistinctUrls();
        var start = -1;
        for (var i = 0; i < all.Count; i++)
        {
            if (string.Equals(all[i], currentUrl, StringComparison.OrdinalIgnoreCase))
            {
                start = i;
                break;
            }
        }
        if (start < 0)
        {
            foreach (var url in all)
            {
                yield return url;
            }

            yield break;
        }

        for (var i = 1; i < all.Count; i++)
        {
            yield return all[(start + i) % all.Count];
        }
    }

    private static IEnumerable<string> Rotated(IReadOnlyList<string> urls, int seed)
    {
        if (urls.Count == 0)
        {
            yield break;
        }

        var start = (int)((uint)seed % (uint)urls.Count);
        for (var i = 0; i < urls.Count; i++)
        {
            yield return urls[(start + i) % urls.Count];
        }
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
