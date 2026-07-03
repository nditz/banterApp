namespace BanterApp.Api.Data.Entities;

public class UserPrediction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PredictionType { get; set; } = string.Empty;
    public Guid? CountryId { get; set; }
    public Guid? PlayerId { get; set; }
    public string? Competition { get; set; }
    public string? Season { get; set; }
    public string? PredictionValue { get; set; }
    public int? Confidence { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
    public bool IsLocked { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public Country? Country { get; set; }
    public Player? Player { get; set; }
}
