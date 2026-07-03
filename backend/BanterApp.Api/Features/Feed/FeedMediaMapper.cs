using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Feed;

public static class FeedMediaMapper
{
    public static FeedMediaResponse? FromNewsItem(NewsFeedItem item)
    {
        if (string.IsNullOrWhiteSpace(item.ImageUrl))
        {
            return null;
        }

        // Older rows may still hold expired DALL-E image URLs that render as a broken
        // "content not available" tile. Serve a stable catalog GIF instead. Giphy/Tenor
        // CDN URLs are stable links and are intentionally persisted.
        if (IsExpiringHost(item.ImageUrl))
        {
            return new FeedMediaResponse(
                "gif",
                FeedGifCatalog.ResolveGifUrl("news", item.Id.GetHashCode()),
                item.Title);
        }

        var type = NormalizeType(item.MediaType);
        return new FeedMediaResponse(type, item.ImageUrl, item.Title);
    }

    private static bool IsExpiringHost(string url) =>
        url.Contains("oaidalleapiprodscus", StringComparison.OrdinalIgnoreCase);

    public static FeedMediaResponse FromGifMood(string mood, string alt) =>
        new("gif", FeedGifCatalog.ResolveGifUrl(mood), alt);

    /// <summary>Deterministic GIF per <paramref name="seed"/> so a given card stays stable but differs from others.</summary>
    public static FeedMediaResponse FromGifMood(string mood, string alt, int seed) =>
        new("gif", FeedGifCatalog.ResolveGifUrl(mood, seed), alt);

    public static FeedMediaResponse FromImageUrl(string url, string alt) =>
        new("image", url, alt);

    private static string NormalizeType(string? mediaType) =>
        string.Equals(mediaType, "gif", StringComparison.OrdinalIgnoreCase) ? "gif" : "image";
}
