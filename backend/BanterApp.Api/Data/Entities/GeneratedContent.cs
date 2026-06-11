namespace BanterApp.Api.Data.Entities;

public enum GeneratedContentType
{
    Analyze,
    Banter,
    Meme,
    VideoScript
}

public class GeneratedContent
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AnonymousUserId { get; set; }
    public GeneratedContentType Type { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
    public AnonymousUser? AnonymousUser { get; set; }
}
