namespace BanterApp.Api.Data.Entities;

public class Player
{
    public Guid Id { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalProvider { get; set; }
    public Guid? CountryId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? KnownName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Position { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ClubName { get; set; }
    public string? NationalTeamName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Country? Country { get; set; }
    public ICollection<PlayerStat> Stats { get; set; } = [];
    public ICollection<LeaderboardEntry> LeaderboardEntries { get; set; } = [];
    public ICollection<UserPrediction> UserPredictions { get; set; } = [];
}
