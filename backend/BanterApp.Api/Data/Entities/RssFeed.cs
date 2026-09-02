namespace BanterApp.Api.Data.Entities;

public static class RssFeedKind
{
    public const string Podcast = "podcast";
    public const string Website = "website";
}

/// <summary>
/// Runtime RSS catalog. Show/outlet identity lives here; <see cref="RssUrl"/> is the mutable pointer.
/// Config seeds this table and must not overwrite a URL the resolver already updated.
/// </summary>
public class RssFeed
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = RssFeedKind.Website;
    public string RssUrl { get; set; } = string.Empty;
    public long? ApplePodcastId { get; set; }
    public string? SiteUrl { get; set; }
    public string? StyleSlug { get; set; }
    public int Priority { get; set; }
    public bool ExtractPredictions { get; set; } = true;
    public bool UseForMediaIngest { get; set; }
    public bool UseForNews { get; set; }
    public bool UseForPundit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastCheckedAt { get; set; }
    public int? LastHttpStatus { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
