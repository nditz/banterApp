namespace BanterApp.Api.Data.Entities;

/// <summary>
/// Persistent take↔outcome record. Private by default — never placed on the public timeline.
/// </summary>
public class PredictionReceipt
{
    public Guid Id { get; set; }
    public Guid PredictionId { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? AnonymousUserId { get; set; }
    public string ResultHash { get; set; } = string.Empty;
    public PredictionType PredictionType { get; set; }
    public string PredictionValue { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
    public int AuraDelta { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string MatchStatus { get; set; } = string.Empty;
    public string StoryType { get; set; } = string.Empty;
    public string StoryTypesJson { get; set; } = "[]";
    public string PunditTakesJson { get; set; } = "[]";
    public bool IsPublic { get; set; }
    public DateTimeOffset SettledAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Prediction Prediction { get; set; } = null!;
    public Match Match { get; set; } = null!;
    public User? User { get; set; }
    public AnonymousUser? AnonymousUser { get; set; }
    public ICollection<ReceiptStoryCandidate> StoryCandidates { get; set; } = [];
}
