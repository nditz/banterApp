namespace BanterApp.Api.Data.Entities;

public class StandingRow
{
    public Guid Id { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public int Rank { get; set; }
    public string TeamCode { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDiff { get; set; }
    public int Points { get; set; }
    public string Provider { get; set; } = "api_football";
    public DateTimeOffset LastSyncedAt { get; set; }
}
