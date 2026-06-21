namespace BanterApp.Api.Data.Entities;

public class MediaItem
{
    public Guid Id { get; set; }
    public Guid MediaSourceId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? TranscriptSnippet { get; set; }
    public string? Author { get; set; }
    public string? Publication { get; set; }
    public string? RawSummary { get; set; }
    public string? RawText { get; set; }
    public string? RawPayloadJson { get; set; }
    public string? ContentHash { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string ProcessingStatus { get; set; } = MediaItemProcessingStatus.Pending;
    public string? ProcessingError { get; set; }
    public DateTimeOffset LastSyncedAt { get; set; }

    public MediaSource MediaSource { get; set; } = null!;
    public ICollection<PunditOpinion> Opinions { get; set; } = [];
}
