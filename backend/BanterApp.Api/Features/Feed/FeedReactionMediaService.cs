using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Services;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Feed;

/// <summary>
/// Presents feed reaction media from the stored library URL. Live Giphy lookups happen at
/// write time and on the staggered GIF refresh job, not on every timeline request.
/// </summary>
public sealed class FeedReactionMediaService
{
    private readonly ReactionMediaResolver _resolver;
    private readonly IReactionGifProvider _gifProvider;
    private readonly IProviderUsageGuard? _usage;
    private readonly ReactionGifOptions _gifOptions;

    public FeedReactionMediaService(
        ReactionMediaResolver resolver,
        IReactionGifProvider gifProvider,
        IProviderUsageGuard? usage = null,
        IOptions<ReactionGifOptions>? gifOptions = null)
    {
        _resolver = resolver;
        _gifProvider = gifProvider;
        _usage = usage;
        _gifOptions = gifOptions?.Value ?? new ReactionGifOptions();
    }

    public bool LiveGifsEnabled => _gifProvider.IsEnabled;

    /// <summary>Maps a persisted feed row to API media without a live GIF round-trip.</summary>
    public Task<FeedMediaResponse?> PresentAsync(
        NewsFeedItem item,
        string title,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(FeedMediaMapper.FromNewsItem(item) is { } mapped
            ? mapped with { Alt = title }
            : null);
    }

    /// <summary>
    /// Replaces bundled sticker URLs on stored feed rows with live Giphy GIF URLs (when enabled
    /// and under daily quota). Used by staggered jobs, not the timeline read path.
    /// </summary>
    public async Task<int> UpgradeStoredStickersAsync(
        IEnumerable<NewsFeedItem> items,
        CancellationToken cancellationToken = default,
        int? maxUpgrades = null)
    {
        if (!LiveGifsEnabled)
        {
            return 0;
        }

        var cap = Math.Clamp(maxUpgrades ?? 2, 0, 8);
        if (cap == 0)
        {
            return 0;
        }

        var upgraded = 0;
        foreach (var item in items)
        {
            if (upgraded >= cap)
            {
                break;
            }

            if (!FeedGifCatalog.IsBundledSticker(item.ImageUrl))
            {
                continue;
            }

            if (_usage is not null &&
                !await _usage.CanInvokeAsync(_gifOptions.UsageProviderName, 1, cancellationToken))
            {
                break;
            }

            var title = FeedBanterFormat.Strip(item.Title);
            var mood = InferMood(item.Category);
            var queries = BuildSearchQueries(title, item.Summary, item.Author, item.Category);
            var resolved = await _resolver.ResolveAsync(
                queries,
                mood,
                item.Id.GetHashCode(),
                cancellationToken);

            if (FeedGifCatalog.IsBundledSticker(resolved.Url))
            {
                continue;
            }

            item.ImageUrl = resolved.Url;
            item.MediaType = resolved.Type;
            upgraded++;
        }

        return upgraded;
    }

    public static IEnumerable<string?> BuildSearchQueries(
        string title,
        string? summary,
        string? author,
        string? category)
    {
        var queries = new List<string?>();

        if (!string.IsNullOrWhiteSpace(title))
        {
            queries.Add($"{StripEmoji(title)} reaction");
            queries.Add($"{StripEmoji(title)} football");
        }

        if (!string.IsNullOrWhiteSpace(author) &&
            string.Equals(category, "pundit_quote", StringComparison.OrdinalIgnoreCase))
        {
            queries.Add($"{author.Trim()} pundit reaction");
        }

        if (!string.IsNullOrWhiteSpace(summary))
        {
            var snippet = StripEmoji(summary).Trim();
            if (snippet.Length > 80)
            {
                snippet = snippet[..80];
            }

            if (snippet.Length >= 12)
            {
                queries.Add($"{snippet} soccer gif");
            }
        }

        return queries;
    }

    internal static string InferMood(string? category) =>
        category?.Trim().ToLowerInvariant() switch
        {
            "ai_reaction" => "debate",
            "pundit_quote" => "pundit",
            "match_live" => "hype",
            "match_result" => "celebrate",
            "match_fixture" => "debate",
            "banter" or "meme" => "roast",
            _ => "news",
        };

    private static string StripEmoji(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value.Where(c => c <= 0xFFFF && !char.IsSurrogate(c)).ToArray();
        var cleaned = new string(chars).Trim();
        return string.Join(
            ' ',
            cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
