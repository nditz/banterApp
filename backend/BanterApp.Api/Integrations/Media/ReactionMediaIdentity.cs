using BanterApp.Api.Features.Feed;

namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Stable identity for a reaction visual. Giphy CDN hosts rotate, so GIFs key off media ID;
/// memes and bundled stickers key off their path or URL.
/// </summary>
public static class ReactionMediaIdentity
{
    public static string FromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var trimmed = url.Trim();
        if (FeedGifCatalog.IsBundledSticker(trimmed))
        {
            return trimmed;
        }

        return GiphyGifSelector.FromUrl(trimmed) ?? trimmed;
    }
}
