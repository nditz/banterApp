namespace BanterApp.Api.Data.Entities;

public class LeagueMember
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AnonymousUserId { get; set; }

    /// <summary>The name this player chose for this league (office mates, family, etc).</summary>
    public string DisplayName { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public League League { get; set; } = null!;
    public User? User { get; set; }
    public AnonymousUser? AnonymousUser { get; set; }
}
