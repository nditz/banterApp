using BanterApp.Api.Features.Feed;

namespace BanterApp.Api.Integrations.Media;

public sealed record ReactionMedia(string Type, string Url);

/// <summary>
/// Resolves the visual for a feed reaction. Prefers a live GIF from the reaction-GIF provider
/// (Tenor) using AI-suggested search phrases, and falls back to the bundled local reaction
/// sticker repository (<see cref="FeedGifCatalog"/>) when the provider is disabled or has no match.
/// </summary>
public sealed class ReactionMediaResolver
{
    private readonly IReactionGifProvider _gifProvider;
    private readonly ILogger<ReactionMediaResolver> _logger;

    public ReactionMediaResolver(
        IReactionGifProvider gifProvider,
        ILogger<ReactionMediaResolver> logger)
    {
        _gifProvider = gifProvider;
        _logger = logger;
    }

    /// <summary>
    /// Resolves a reaction GIF. <paramref name="aiQueries"/> are AI-suggested search phrases
    /// (best first); <paramref name="mood"/> is used both to derive a fallback query and to
    /// pick a local sticker if the provider yields nothing. <paramref name="seed"/> keeps a
    /// given card stable while varying across cards.
    /// </summary>
    public async Task<ReactionMedia> ResolveAsync(
        IEnumerable<string?>? aiQueries,
        string? mood,
        int seed,
        CancellationToken cancellationToken = default)
    {
        if (_gifProvider.IsEnabled)
        {
            foreach (var query in BuildQueries(aiQueries, mood))
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

        // Fallback: bundled local reaction sticker repository.
        return new ReactionMedia("gif", FeedGifCatalog.ResolveGifUrl(mood, seed));
    }

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

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        // Bias generic phrases toward football/soccer reaction GIFs.
        if (trimmed.Length < 30 &&
            !trimmed.Contains("football", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Contains("soccer", StringComparison.OrdinalIgnoreCase))
        {
            return $"{trimmed} soccer";
        }

        return trimmed;
    }

    private static string MoodToQuery(string? mood) =>
        (mood?.Trim().ToLowerInvariant()) switch
        {
            "celebrate" => "soccer celebration",
            "win" => "football win celebration",
            "hype" => "football hype crowd",
            "debate" => "sports argument reaction",
            "shock" => "shocked football fan",
            "chaos" => "chaotic celebration soccer",
            "facepalm" => "facepalm reaction",
            "miss" => "disappointed football fan",
            "roast" => "laughing pointing reaction",
            "trophy" => "trophy celebration football",
            "news" => "breaking news reaction",
            "pundit" => "sports pundit talking",
            "cooked" => "cooked reaction",
            "ratio" => "laughing reaction",
            "delulu" => "delusional reaction",
            "maincharacter" => "confident walk soccer",
            _ => "football reaction",
        };
}
