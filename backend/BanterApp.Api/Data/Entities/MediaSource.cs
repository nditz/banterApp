namespace BanterApp.Api.Data.Entities;

public class MediaSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? RssUrl { get; set; }
    public string? SiteUrl { get; set; }
    public bool CrawlAllowed { get; set; }
    public bool ExtractPredictions { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? ConfigJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<MediaItem> Items { get; set; } = [];
}
