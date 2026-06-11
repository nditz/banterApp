namespace BanterApp.Api.Data.Entities;

public class Match
{
    public string Id { get; set; } = string.Empty;
    public string TeamA { get; set; } = string.Empty;
    public string TeamB { get; set; } = string.Empty;
    public string TeamACode { get; set; } = string.Empty;
    public string TeamBCode { get; set; } = string.Empty;
    public DateTimeOffset KickoffTime { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string Status { get; set; } = "NS";
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = [];
    public ICollection<PunditPrediction> PunditPredictions { get; set; } = [];
}
