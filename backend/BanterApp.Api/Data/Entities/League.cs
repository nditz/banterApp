namespace BanterApp.Api.Data.Entities;

public class League
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User CreatedByUser { get; set; } = null!;
    public ICollection<LeagueMember> Members { get; set; } = [];
}
