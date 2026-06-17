namespace BanterApp.Api.Data.Entities;

public class TournamentBonusPick
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AnonymousUserId { get; set; }
    public TournamentBonusCategory Category { get; set; }
    public string PickValue { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public User? User { get; set; }
    public AnonymousUser? AnonymousUser { get; set; }
}
