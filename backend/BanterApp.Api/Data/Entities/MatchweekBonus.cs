namespace BanterApp.Api.Data.Entities;

public class MatchweekBonus
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AnonymousUserId { get; set; }
    public Guid? CompetitionSeasonId { get; set; }
    public int MatchweekNumber { get; set; }
    public int PointsAwarded { get; set; }
    public DateTimeOffset AwardedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
    public AnonymousUser? AnonymousUser { get; set; }
}
