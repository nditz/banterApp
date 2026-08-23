namespace BanterApp.Api.Data.Entities;

public class SeasonTeam
{
    public Guid Id { get; set; }
    public Guid CompetitionSeasonId { get; set; }
    public Guid TeamId { get; set; }

    public CompetitionSeason CompetitionSeason { get; set; } = null!;
    public ClubTeam Team { get; set; } = null!;
}
