using BanterApp.Api.Features.Feed;

namespace BanterApp.Api.Integrations.Media;

public sealed record ReactionMedia(string Type, string Url);

/// <summary>
/// Resolves the visual for a feed reaction or meme. Prefers a live Giphy GIF, then a
/// bundled sticker that has not already been shown in the Friday–Monday window.
/// </summary>
public sealed class ReactionMediaResolver
{
    private readonly IReactionGifProvider _gifProvider;
    private readonly IReactionGifLedger _ledger;
    private readonly ILogger<ReactionMediaResolver> _logger;

    public ReactionMediaResolver(
        IReactionGifProvider gifProvider,
        IReactionGifLedger ledger,
        ILogger<ReactionMediaResolver> logger)
    {
        _gifProvider = gifProvider;
        _ledger = ledger;
        _logger = logger;
    }

    /// <summary>
    /// Resolves a reaction GIF or meme sticker. <paramref name="aiQueries"/> are AI-suggested
    /// search phrases (best first); <paramref name="mood"/> is used both to derive a fallback
    /// query and to pick a local sticker if the provider yields nothing. <paramref name="seed"/>
    /// keeps a given card on the first unique visual assigned in the current Friday–Monday window.
    /// </summary>
    public async Task<ReactionMedia> ResolveAsync(
        IEnumerable<string?>? aiQueries,
        string? mood,
        int seed,
        CancellationToken cancellationToken = default)
    {
        var assigned = await _ledger.GetAssignedUrlAsync(seed, cancellationToken);
        if (!string.IsNullOrWhiteSpace(assigned))
        {
            return new ReactionMedia(MediaTypeFor(assigned), assigned);
        }

        if (_gifProvider.IsEnabled)
        {
            var queries = BuildQueries(aiQueries, mood).ToList();
            foreach (var query in Rotate(queries, seed))
            {
                try
                {
                    var url = await _gifProvider.FindGifUrlAsync(query, seed, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return new ReactionMedia("gif", url);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Reaction GIF lookup failed for query '{Query}'.", query);
                }
            }
        }

        foreach (var sticker in FeedGifCatalog.Candidates(mood, seed))
        {
            var id = ReactionMediaIdentity.FromUrl(sticker);
            if (await _ledger.TryClaimAsync(seed, id, sticker, cancellationToken))
            {
                return new ReactionMedia("gif", sticker);
            }
        }

        var fallback = FeedGifCatalog.ResolveGifUrl(mood, seed);
        await _ledger.TryClaimAsync(seed, ReactionMediaIdentity.FromUrl(fallback), fallback, cancellationToken);
        return new ReactionMedia("gif", fallback);
    }

    private static string MediaTypeFor(string url) =>
        url.Contains("giphy.com", StringComparison.OrdinalIgnoreCase) ||
        FeedGifCatalog.IsBundledSticker(url)
            ? "gif"
            : "image";

    private static IEnumerable<string> BuildQueries(IEnumerable<string?>? aiQueries, string? mood)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (aiQueries is not null)
        {
            foreach (var raw in aiQueries)
            {
                var query = Clean(raw);
                if (query is not null && seen.Add(query))
                {
                    yield return query;
                }
            }
        }

        var moodQuery = MoodToQuery(mood);
        if (seen.Add(moodQuery))
        {
            yield return moodQuery;
        }
    }

    /// <summary>
    /// Starts at a seed-derived query so cards with the same AI phrase list do not all
    /// lock onto the first successful Giphy search.
    /// </summary>
    private static IEnumerable<string> Rotate(IReadOnlyList<string> queries, int seed)
    {
        if (queries.Count == 0)
        {
            yield break;
        }

        var start = (int)((uint)seed % (uint)queries.Count);
        for (var i = 0; i < queries.Count; i++)
        {
            yield return queries[(start + i) % queries.Count];
        }
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length < 40 &&
            !trimmed.Contains("football", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Contains("soccer", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Contains("goal", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Contains("celebration", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Contains("meme", StringComparison.OrdinalIgnoreCase))
        {
            return $"{trimmed} football meme";
        }

        return trimmed;
    }

    private static string MoodToQuery(string? mood) =>
        (mood?.Trim().ToLowerInvariant()) switch
        {
            "celebrate" => "soccer celebration meme",
            "win" => "football win celebration",
            "hype" => "football hype meme",
            "debate" => "sports argument meme",
            "shock" => "shocked football fan meme",
            "chaos" => "chaotic celebration soccer",
            "facepalm" => "facepalm football meme",
            "miss" => "disappointed football fan meme",
            "roast" => "football roast meme",
            "trophy" => "trophy celebration football",
            "news" => "breaking news reaction meme",
            "pundit" => "sports pundit meme",
            "cooked" => "cooked reaction meme",
            "ratio" => "laughing football meme",
            "delulu" => "delusional football meme",
            "maincharacter" => "confident walk soccer",
            "meme" => "football meme",
            _ => "football meme",
        };
}
