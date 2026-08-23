namespace BanterApp.Api.Data.Entities;

public class Matchweek
{
    public Guid Id { get; set; }
    public Guid CompetitionSeasonId { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string Status { get; set; } = "scheduled";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CompetitionSeason CompetitionSeason { get; set; } = null!;
    public ICollection<Match> Matches { get; set; } = [];
}
