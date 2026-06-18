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

        var type = NormalizeType(item.MediaType);
        return new FeedMediaResponse(type, item.ImageUrl, item.Title);
    }

    public static FeedMediaResponse FromGifMood(string mood, string alt) =>
        new("gif", FeedGifCatalog.ResolveGifUrl(mood), alt);

    public static FeedMediaResponse FromImageUrl(string url, string alt) =>
        new("image", url, alt);

    private static string NormalizeType(string? mediaType) =>
        string.Equals(mediaType, "gif", StringComparison.OrdinalIgnoreCase) ? "gif" : "image";
}
