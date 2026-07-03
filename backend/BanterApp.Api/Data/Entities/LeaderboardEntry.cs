namespace BanterApp.Api.Data.Entities;

public class LeaderboardEntry
{
    public Guid Id { get; set; }
    public string LeaderboardType { get; set; } = string.Empty;
    public Guid PlayerId { get; set; }
    public Guid? CountryId { get; set; }
    public int? Rank { get; set; }
    public decimal Value { get; set; }
    public string? Competition { get; set; }
    public string? Season { get; set; }
    public string? SourceProvider { get; set; }
    public DateTimeOffset? SourceUpdatedAt { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Player Player { get; set; } = null!;
    public Country? Country { get; set; }
}
