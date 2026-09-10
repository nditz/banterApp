namespace BanterApp.Api.Features.Metrics;

/// <summary>
/// Product funnel metrics. The keys are an allowlist: anything not listed here is rejected
/// so the endpoint cannot be used as a general-purpose tracking sink.
/// </summary>
public static class ProductMetrics
{
    public const string PredictionMade = "prediction_made";
    public const string ReceiptViewed = "receipt_viewed";
    public const string StudioOpened = "studio_opened";
    public const string ContentGenerated = "content_generated";
    public const string ContentExported = "content_exported";
    public const string LeagueJoined = "league_joined";
    public const string PunditFollowed = "pundit_followed";
    public const string ReturnedAfterResult = "returned_after_result";
    public const string AdInitFailed = "ad_init_failed";
    public const string AdSlotFilled = "ad_slot_filled";
    public const string AdSlotUnfilled = "ad_slot_unfilled";

    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        PredictionMade,
        ReceiptViewed,
        StudioOpened,
        ContentGenerated,
        ContentExported,
        LeagueJoined,
        PunditFollowed,
        ReturnedAfterResult,
        AdInitFailed,
        AdSlotFilled,
        AdSlotUnfilled
    };

    public static bool IsAllowed(string? key) =>
        !string.IsNullOrWhiteSpace(key) && Allowed.Contains(key);
}
