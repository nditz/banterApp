namespace BanterApp.Api.Data.Entities;

/// <summary>Official tournament award answers used to score bonus picks after the tournament.</summary>
public class TournamentAwardResult
{
    public Guid Id { get; set; }
    public TournamentBonusCategory Category { get; set; }
    public string AnswerValue { get; set; } = string.Empty;
    public string? AnswerDisplay { get; set; }
    public DateTimeOffset AnnouncedAt { get; set; } = DateTimeOffset.UtcNow;
}
