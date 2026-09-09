using BanterApp.Api.Common;

namespace BanterApp.Api.Data.Entities;

/// <summary>
/// First-party reaction visual we are allowed to store and serve (bundled stickers or
/// later our own licensed CDN). Giphy/Tenor media files are not stored here.
/// </summary>
public class GifAsset
{
    public Guid Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Mood { get; set; }

    public string? Tags { get; set; }

    public string Source { get; set; } = GifAssetSources.Bundled;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public static class GifAssetSources
{
    public const string Bundled = "bundled";
    public const string Licensed = "licensed";
}

public static class GifAssetLimits
{
    public const int Url = StringLimits.ReactionGifUrl;
    public const int Title = StringLimits.GifAssetTitle;
    public const int Description = StringLimits.GifAssetDescription;
    public const int Mood = StringLimits.GifAssetMood;
    public const int Tags = StringLimits.GifAssetTags;
    public const int Source = StringLimits.GifAssetSource;
}
