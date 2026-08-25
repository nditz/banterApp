namespace BanterApp.Api.Features.Analytics;

public sealed record AnalyticsEventDefinition(
    string Name,
    string Feature,
    IReadOnlySet<string> AllowedProperties);

/// <summary>
/// The authoritative allowlist of product analytics events. An event name that is not
/// listed is rejected; a property key that is not listed for its event is dropped.
/// Adding an entry here is a deliberate privacy decision, so keep properties coarse and
/// never introduce free-form text, identifiers of other users, or content bodies.
/// </summary>
public static class AnalyticsEventCatalog
{
    public const int MaxBatchSize = 20;
    public const int MaxPropertyValueLength = 64;
    public const int MaxPropertiesPerEvent = 8;

    private static IReadOnlySet<string> Props(params string[] keys) =>
        new HashSet<string>(keys, StringComparer.Ordinal);

    private static readonly AnalyticsEventDefinition[] Definitions =
    [
        // Acquisition and activation.
        new("session_started", "acquisition", Props("isReturning")),
        new("landing_viewed", "acquisition", Props("variant")),
        new("guest_session_created", "acquisition", Props("countryCode")),
        // Records only that a key was produced. The key value must never be sent.
        new("recovery_key_created", "acquisition", Props()),

        // Authentication.
        new("registration_started", "auth", Props("method")),
        new("registration_completed", "auth", Props("method")),
        new("login_completed", "auth", Props("method")),
        new("guest_claim_completed", "auth", Props("predictionsClaimed")),

        // Prediction engagement.
        new("fixture_viewed", "prediction", Props("matchweek")),
        new("prediction_started", "prediction", Props("matchweek", "predictionType")),
        new("prediction_created", "prediction", Props("matchweek", "predictionType")),
        new("prediction_updated", "prediction", Props("matchweek", "predictionType")),
        new("matchweek_predictions_completed", "prediction", Props("matchweek", "predictionCount")),
        new("prediction_result_viewed", "prediction", Props("matchweek")),
        new("leaderboard_viewed", "prediction", Props("scope")),

        // Private leagues.
        new("prediction_league_created", "league", Props("kind")),
        new("prediction_league_joined", "league", Props("kind")),
        new("prediction_league_viewed", "league", Props("kind")),

        // Pundits.
        new("pundit_list_viewed", "pundit", Props()),
        new("pundit_profile_viewed", "pundit", Props("punditId")),
        new("pundit_comparison_viewed", "pundit", Props("matchweek")),
        new("pundit_source_opened", "pundit", Props("sourceType")),

        // AI content. Never carries prompts or generated output.
        new("content_generation_started", "content", Props("contentType", "tone")),
        new("content_generation_completed", "content", Props("contentType", "tone", "durationBucket")),
        new("content_generation_failed", "content", Props("contentType", "errorCategory")),
        new("content_regenerated", "content", Props("contentType")),
        new("content_exported", "content", Props("contentType", "exportFormat"))
    ];

    private static readonly Dictionary<string, AnalyticsEventDefinition> ByName =
        Definitions.ToDictionary(d => d.Name, StringComparer.Ordinal);

    public static IReadOnlyList<AnalyticsEventDefinition> All => Definitions;

    public static AnalyticsEventDefinition? Find(string? eventName) =>
        !string.IsNullOrWhiteSpace(eventName) && ByName.TryGetValue(eventName, out var definition)
            ? definition
            : null;

    public static bool IsKnown(string? eventName) => Find(eventName) is not null;
}
