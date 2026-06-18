namespace BanterApp.Api.Data.Entities;

/// <summary>
/// Where a pundit take was scraped or cited from (YouTube/podcast APIs later).
/// </summary>
public static class PunditSourcePlatform
{
    public const string Podcast = "podcast";
    public const string YouTube = "youtube";
    public const string Article = "article";
    public const string Tv = "tv";
    public const string Social = "social";
    public const string Manual = "manual";

    public static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        Podcast, YouTube, Article, Tv, Social, Manual
    };

    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) || !Known.Contains(value.Trim())
            ? null
            : value.Trim().ToLowerInvariant();
}
