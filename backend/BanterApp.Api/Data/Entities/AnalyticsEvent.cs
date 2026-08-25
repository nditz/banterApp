namespace BanterApp.Api.Data.Entities;

/// <summary>
/// A single first-party product analytics event. Deliberately carries no IP address,
/// user agent, referrer or free-form content; see docs/balltakes-admin/ANALYTICS_MODEL.md.
/// </summary>
public class AnalyticsEvent
{
    public Guid Id { get; set; }

    /// <summary>Validated against <c>AnalyticsEventCatalog</c> before insert.</summary>
    public string EventName { get; set; } = string.Empty;

    public string Feature { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? UserId { get; set; }

    public Guid? AnonymousSessionId { get; set; }

    /// <summary>Allowlisted, sanitized, primitive-only properties. Null when empty.</summary>
    public string? PropertiesJson { get; set; }

    public string? AppVersion { get; set; }

    public string Environment { get; set; } = string.Empty;

    public string? CountryCode { get; set; }
}
