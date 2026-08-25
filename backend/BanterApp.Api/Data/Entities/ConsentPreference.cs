namespace BanterApp.Api.Data.Entities;

/// <summary>
/// A visitor's privacy choices. Keyed by the registered user where one exists, otherwise
/// by the guest session. The "necessary" category is implicit and never stored, because
/// it cannot be declined without breaking the service.
/// </summary>
public class ConsentPreference
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? AnonymousUserId { get; set; }

    /// <summary>Version of the consent notice the choice was made against.</summary>
    public string ConsentVersion { get; set; } = string.Empty;

    public bool AnalyticsAllowed { get; set; }

    public bool MarketingAllowed { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }

    public AnonymousUser? AnonymousUser { get; set; }
}
