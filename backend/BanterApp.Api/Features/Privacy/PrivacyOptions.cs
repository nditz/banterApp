namespace BanterApp.Api.Features.Privacy;

public sealed class PrivacyOptions
{
    public const string SectionName = "Privacy";

    /// <summary>
    /// Bumped whenever the consent notice changes materially. A stored choice made
    /// against an older version is treated as stale and the banner is shown again.
    /// </summary>
    public string ConsentVersion { get; set; } = "2026-08-25";

    /// <summary>
    /// When false, the analytics category cannot be granted at all. Used to disable
    /// product analytics for an environment without changing client code.
    /// </summary>
    public bool AnalyticsEnabled { get; set; } = true;

    /// <summary>
    /// The marketing category currently covers the AdSense loader only. No behavioural
    /// advertising or cross-site tracking is installed.
    /// </summary>
    public bool MarketingCategoryEnabled { get; set; } = true;
}
