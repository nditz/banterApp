namespace BanterApp.Api.Data.Entities;

public class LeagueMember
{
    public Guid LeagueId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public League League { get; set; } = null!;
    public User User { get; set; } = null!;
}
