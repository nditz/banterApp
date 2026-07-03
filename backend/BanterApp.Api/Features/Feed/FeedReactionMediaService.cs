using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Media;

namespace BanterApp.Api.Features.Feed;

/// <summary>
/// Presents feed reaction media: upgrades bundled <c>/reactions/</c> stickers to live Giphy GIFs
/// when configured, and builds search queries from card text for richer matches.
/// </summary>
public sealed class FeedReactionMediaService
{
    private readonly ReactionMediaResolver _resolver;
    private readonly IReactionGifProvider _gifProvider;

    public FeedReactionMediaService(
        ReactionMediaResolver resolver,
        IReactionGifProvider gifProvider)
    {
        _resolver = resolver;
        _gifProvider = gifProvider;
    }

    public bool LiveGifsEnabled => _gifProvider.IsEnabled;

    /// <summary>Maps a persisted feed row to API media, upgrading local stickers when Giphy is live.</summary>
    public async Task<FeedMediaResponse?> PresentAsync(
        NewsFeedItem item,
        string title,
        CancellationToken cancellationToken = default)
    {
        var mapped = FeedMediaMapper.FromNewsItem(item);
        if (mapped is null)
        {
            return null;
        }

        if (!FeedGifCatalog.IsBundledSticker(mapped.Url) || !LiveGifsEnabled)
        {
            return mapped;
        }

        var mood = InferMood(item.Category);
        var queries = BuildSearchQueries(title, item.Summary, item.Author, item.Category);
        var resolved = await _resolver.ResolveAsync(
            queries,
            mood,
            item.Id.GetHashCode(),
            cancellationToken);

        if (FeedGifCatalog.IsBundledSticker(resolved.Url))
        {
            return mapped;
        }

        return new FeedMediaResponse(resolved.Type, resolved.Url, title);
    }

    /// <summary>
    /// Replaces bundled sticker URLs on stored feed rows with live Giphy GIF URLs (when enabled).
    /// </summary>
    public async Task<int> UpgradeStoredStickersAsync(
        IEnumerable<NewsFeedItem> items,
        CancellationToken cancellationToken = default)
    {
        if (!LiveGifsEnabled)
        {
            return 0;
        }

        var upgraded = 0;
        foreach (var item in items)
        {
            if (!FeedGifCatalog.IsBundledSticker(item.ImageUrl))
            {
                continue;
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
