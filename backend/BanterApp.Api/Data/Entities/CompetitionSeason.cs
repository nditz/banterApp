namespace BanterApp.Api.Data.Entities;

public class CompetitionSeason
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StartYear { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? ProviderSeasonId { get; set; }
    public string Status { get; set; } = "current";
    public bool IsCurrent { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Competition Competition { get; set; } = null!;
    public ICollection<Matchweek> Matchweeks { get; set; } = [];
    public ICollection<SeasonTeam> Teams { get; set; } = [];
    public ICollection<Match> Matches { get; set; } = [];
}
