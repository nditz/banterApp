using BanterApp.Api.Common;

namespace BanterApp.Api.Data.Entities;

/// <summary>
/// Search phrases for live GIF lookups (curated or Giphy trending terms). Stores text only —
/// not Giphy media files or a GIF index.
/// </summary>
public class GifSearchQuery
{
    public Guid Id { get; set; }

    public string Phrase { get; set; } = string.Empty;

    public string Source { get; set; } = GifSearchQuerySources.Curated;

    public bool IsFootballRelated { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset LastSeenAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public static class GifSearchQuerySources
{
    public const string Curated = "curated";
    public const string GiphyTrending = "giphy_trending";
    public const string GiphyTags = "giphy_tags";
}

public static class GifSearchQueryLimits
{
    public const int Phrase = StringLimits.GifSearchPhrase;
    public const int Source = StringLimits.GifSearchSource;
}
