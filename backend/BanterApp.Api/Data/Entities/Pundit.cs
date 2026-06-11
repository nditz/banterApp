namespace BanterApp.Api.Data.Entities;

public class Pundit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;

    public ICollection<PunditPrediction> Predictions { get; set; } = [];
}
