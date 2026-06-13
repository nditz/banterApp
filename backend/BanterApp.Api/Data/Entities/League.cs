namespace BanterApp.Api.Data.Entities;

public class League
{
    public const int DefaultMaxMembers = 50;

    /// <summary>Family / office / friends leagues a user creates or joins via invite.</summary>
    public const int MaxCustomLeaguesPerUser = 3;

    /// <summary>Custom + global + country memberships combined.</summary>
    public const int MaxTotalLeagueMemberships = 5;

    public static readonly Guid GlobalLeagueId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public LeagueKind Kind { get; set; } = LeagueKind.Custom;

    /// <summary>ISO 3166-1 alpha-2 for <see cref="LeagueKind.Country"/> leagues.</summary>
    public string? CountryCode { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? CreatedByAnonymousUserId { get; set; }
    public int MaxMembers { get; set; } = DefaultMaxMembers;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? CreatedByUser { get; set; }
    public AnonymousUser? CreatedByAnonymousUser { get; set; }
    public ICollection<LeagueMember> Members { get; set; } = [];
}
