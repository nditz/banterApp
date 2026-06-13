namespace BanterApp.Api.Data.Entities;

public class MatchEvent
{
    public Guid Id { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public int Minute { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? TeamCode { get; set; }
    public string? PlayerName { get; set; }
    public string? Detail { get; set; }
    public string Provider { get; set; } = "api_football";
    public string ProviderEventId { get; set; } = string.Empty;

    public Match Match { get; set; } = null!;
}
