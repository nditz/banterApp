namespace BanterApp.Api.Data.Entities;

public enum PredictionType
{
    Result = 0,
    CorrectScore = 1,
    DoubleChance = 2
}

public class Prediction
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AnonymousUserId { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public PredictionType PredictionType { get; set; }
    public string PredictionValue { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public User? User { get; set; }
    public AnonymousUser? AnonymousUser { get; set; }
    public Match Match { get; set; } = null!;
}
