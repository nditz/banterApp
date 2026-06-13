namespace BanterApp.Api.Data.Entities;

public class AnonymousUser
{
    public Guid Id { get; set; }
    public string RecoveryCode { get; set; } = string.Empty;
    public string CookieId { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    /// <summary>
    /// SHA-256 prefix of stable browser signals. When a recovery token is used
    /// on a device with a different fingerprint the old cookie is rotated out,
    /// enforcing one active session per key.
    /// </summary>
    public string? DeviceFingerprint { get; set; }
    public int AiGenerationsUsed { get; set; }
    public DateTimeOffset? TermsAcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Prediction> Predictions { get; set; } = [];
    public ICollection<GeneratedContent> GeneratedContents { get; set; } = [];
    public ICollection<BracketPick> BracketPicks { get; set; } = [];
}
