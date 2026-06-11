namespace BanterApp.Api.Data.Entities;

public class AnonymousUser
{
    public Guid Id { get; set; }
    public string RecoveryCode { get; set; } = string.Empty;
    public string CookieId { get; set; } = string.Empty;
    public int AiGenerationsUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Prediction> Predictions { get; set; } = [];
    public ICollection<GeneratedContent> GeneratedContents { get; set; } = [];
}
