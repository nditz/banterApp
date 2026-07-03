namespace BanterApp.Api.Data.Entities;

public class Country
{
    public Guid Id { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalProvider { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? FlagUrl { get; set; }
    public string? Continent { get; set; }
    public int? FifaRanking { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Player> Players { get; set; } = [];
    public ICollection<PlayerStat> PlayerStats { get; set; } = [];
    public ICollection<LeaderboardEntry> LeaderboardEntries { get; set; } = [];
    public ICollection<UserPrediction> UserPredictions { get; set; } = [];
}
