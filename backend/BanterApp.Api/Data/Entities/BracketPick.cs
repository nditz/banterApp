namespace BanterApp.Api.Data.Entities;

public class BracketPick
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AnonymousUserId { get; set; }
    public string SlotId { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;
    public string WinnerTeamCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LockedAt { get; set; }

    public User? User { get; set; }
    public AnonymousUser? AnonymousUser { get; set; }
    public Match Match { get; set; } = null!;
}
