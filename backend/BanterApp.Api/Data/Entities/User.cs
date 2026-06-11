namespace BanterApp.Api.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public bool IsAdultVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Prediction> Predictions { get; set; } = [];
    public ICollection<LeagueMember> LeagueMemberships { get; set; } = [];
    public ICollection<GeneratedContent> GeneratedContents { get; set; } = [];
}
