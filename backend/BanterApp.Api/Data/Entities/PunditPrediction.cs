namespace BanterApp.Api.Data.Entities;

public class PunditPrediction
{
    public Guid Id { get; set; }
    public Guid PunditId { get; set; }
    public string MatchId { get; set; } = string.Empty;
    public string Prediction { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }

    public Pundit Pundit { get; set; } = null!;
    public Match Match { get; set; } = null!;
}
