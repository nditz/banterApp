namespace BanterApp.Api.Data.Entities;

public class Competition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string Provider { get; set; } = "api_football";
    public string? ProviderCompetitionId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAvailableForPrediction { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CompetitionSeason> Seasons { get; set; } = [];
}
