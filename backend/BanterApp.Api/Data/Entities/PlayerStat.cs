namespace BanterApp.Api.Data.Entities;

public class PlayerStat
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid? CountryId { get; set; }
    public string? Competition { get; set; }
    public string? Season { get; set; }
    public int MatchesPlayed { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int MinutesPlayed { get; set; }
    public decimal? Rating { get; set; }
    public string? SourceProvider { get; set; }
    public DateTimeOffset? SourceUpdatedAt { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Player Player { get; set; } = null!;
    public Country? Country { get; set; }
}
