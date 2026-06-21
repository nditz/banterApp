namespace BanterApp.Api.Data.Entities;

public class PunditOpinion
{
    public Guid Id { get; set; }
    public Guid SourceItemId { get; set; }
    public Guid PunditId { get; set; }
    public string? Topic { get; set; }
    public string? Team { get; set; }
    public string? Player { get; set; }
    public string? MatchName { get; set; }
    public string Opinion { get; set; } = string.Empty;
    public string? Prediction { get; set; }
    public string? PredictionType { get; set; }
    public double? Confidence { get; set; }
    public string? EvidenceQuote { get; set; }
    public string? QuoteContext { get; set; }
    public bool IsDirectQuote { get; set; }
    public bool NeedsHumanReview { get; set; }
    public string ReviewStatus { get; set; } = "pending";
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ExtractedJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public MediaItem SourceItem { get; set; } = null!;
    public Pundit Pundit { get; set; } = null!;
}
