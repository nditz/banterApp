namespace BanterApp.Api.Data.Entities;

public class PunditPrediction
{
    public Guid Id { get; set; }
    public Guid PunditId { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public string Prediction { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }
    public string? SourceType { get; set; }
    public string? SourceUrl { get; set; }
    public string? Author { get; set; }
    public string? Speaker { get; set; }
    public string? PredictionType { get; set; }
    public string? PredictedTeam { get; set; }
    public string? PredictedScore { get; set; }
    public double? Confidence { get; set; }
    public string? EvidenceSnippet { get; set; }
    public bool IsMatched { get; set; } = true;

    public Pundit Pundit { get; set; } = null!;
    public Match? Match { get; set; }
}
