namespace BanterApp.Api.Data.Entities;

public class LineupPlayer
{
    public Guid Id { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public string TeamCode { get; set; } = string.Empty;
    public int? ShirtNumber { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsSubstitute { get; set; }
    public string Provider { get; set; } = "api_football";

    public Match Match { get; set; } = null!;
}
