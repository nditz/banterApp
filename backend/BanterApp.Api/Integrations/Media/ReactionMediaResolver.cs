using BanterApp.Api.Features.Feed;
using BanterApp.Api.Services;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Media;

public sealed record ReactionMedia(string Type, string Url);

/// <summary>
/// Resolves the visual for a feed reaction or meme. Prefers the first-party GIF library,
/// then a throttled live Giphy GIF, then a bundled sticker that has not already been shown
/// in the Friday–Monday window.
/// </summary>
public sealed class ReactionMediaResolver
{
    private readonly IReactionGifProvider _gifProvider;
    private readonly IReactionGifLedger _ledger;
    private readonly ILogger<ReactionMediaResolver> _logger;
    private readonly GifLibraryService? _library;
    private readonly IProviderUsageGuard? _usage;
    private readonly ReactionGifOptions _gifOptions;

    public ReactionMediaResolver(
        IReactionGifProvider gifProvider,
        IReactionGifLedger ledger,
        ILogger<ReactionMediaResolver> logger,
        GifLibraryService? library = null,
        IProviderUsageGuard? usage = null,
        IOptions<ReactionGifOptions>? gifOptions = null)
    {
        _gifProvider = gifProvider;
        _ledger = ledger;
        _logger = logger;
        _library = library;
        _usage = usage;
        _gifOptions = gifOptions?.Value ?? new ReactionGifOptions();
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

        var trending = _library is null
            ? Array.Empty<string>()
            : await _library.GetActiveQueriesAsync(8, cancellationToken);
        var queries = BuildQueries(aiQueries, mood, trending).ToList();
        var tryLiveFirst = ShouldTryLiveFirst();

        if (tryLiveFirst)
        {
            var live = await TryLiveAsync(queries, seed, cancellationToken);
            if (live is not null)
            {
                return live;
            }
        }

        if (_library is not null && _gifOptions.PreferLocalLibrary)
        {
            var asset = await _library.PickAsync(
                mood,
                queries,
                (id, url) => _ledger.TryClaimAsync(seed, id, url, cancellationToken),
                cancellationToken);
            if (asset is not null)
            {
                return new ReactionMedia(MediaTypeFor(asset.Url), asset.Url);
            }
        }

        if (!tryLiveFirst)
        {
            var live = await TryLiveAsync(queries, seed, cancellationToken);
            if (live is not null)
            {
                return live;
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

    private bool ShouldTryLiveFirst()
    {
        if (!_gifProvider.IsEnabled)
        {
            return false;
        }

        if (_library is null || !_gifOptions.PreferLocalLibrary)
        {
            return true;
        }

        return Random.Shared.NextDouble() < Math.Clamp(_gifOptions.LiveMixChance, 0, 1);
    }

    private async Task<ReactionMedia?> TryLiveAsync(
        IReadOnlyList<string> queries,
        int seed,
        CancellationToken cancellationToken)
    {
        if (!_gifProvider.IsEnabled || queries.Count == 0)
        {
            return null;
        }

        if (_usage is not null &&
            !await _usage.CanInvokeAsync(_gifOptions.UsageProviderName, 1, cancellationToken))
        {
            return null;
        }

        var lookups = 0;
        var maxLookups = Math.Clamp(_gifOptions.MaxLiveLookupsPerResolve, 1, 6);
        foreach (var query in Rotate(queries, seed))
        {
            if (lookups >= maxLookups)
            {
                break;
            }

            lookups++;
            try
            {
                var started = DateTime.UtcNow;
                var url = await _gifProvider.FindGifUrlAsync(query, seed, cancellationToken);
                if (_usage is not null)
                {
                    await _usage.RecordSuccessAsync(
                        _gifOptions.UsageProviderName,
                        1,
                        (int)(DateTime.UtcNow - started).TotalMilliseconds,
                        cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(url))
                {
                    return new ReactionMedia("gif", url);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (_usage is not null)
                {
                    await _usage.RecordFailureAsync(_gifOptions.UsageProviderName, ex.Message, cancellationToken);
                }

                _logger.LogWarning(ex, "Reaction GIF lookup failed for query '{Query}'.", query);
            }
        }

        return null;
    }

    private static string MediaTypeFor(string url) =>
        url.Contains("giphy.com", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("tenor.com", StringComparison.OrdinalIgnoreCase) ||
        FeedGifCatalog.IsBundledSticker(url)
            ? "gif"
            : "image";

    private static IEnumerable<string> BuildQueries(
        IEnumerable<string?>? aiQueries,
        string? mood,
        IEnumerable<string> trending)
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

        foreach (var phrase in trending)
        {
            var query = Clean(phrase);
            if (query is not null && seen.Add(query))
            {
                yield return query;
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
            !trimmed.Contains("meme", StringComparison.OrdinalIgnoreCase) &&
            !FootballGifQuery.LooksFootball(trimmed))
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
