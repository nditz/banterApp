namespace BanterApp.Api.Integrations.Banter;

public sealed record BanterContext(
    Guid? UserId,
    Guid? PredictionId,
    string? MatchId,
    string? TeamId,
    string? TeamName,
    string? OpponentName,
    PredictionOutcomeKind PredictedOutcome,
    MatchOutcomeKind ActualOutcome,
    int? HomeScore,
    int? AwayScore,
    DateTimeOffset? MatchFinishedAtUtc,
    string? Headline = null,
    string? Summary = null,
    string? Category = null,
    string? MoodHint = null,
    bool? PredictionCorrect = null);

public sealed record BanterConcept(string Phrase, string? Tone = null);

public sealed record BanterCandidate(
    string Provider,
    string ProviderContentId,
    BanterContentType ContentType,
    string SourceQuery,
    string Url,
    double ProviderRank,
    IReadOnlyCollection<string> Tags);

public sealed record ScoredBanterCandidate(
    BanterCandidate Candidate,
    double Relevance,
    double Freshness,
    double Popularity,
    double Novelty,
    double FinalScore);

public sealed class BanterExclusionContext
{
    public HashSet<string> ProviderContentIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> MemeTemplateIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SearchPhrases { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> CaptionHashes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static BanterExclusionContext Empty { get; } = new();

    public bool IsProviderIdExcluded(string? providerContentId) =>
        !string.IsNullOrWhiteSpace(providerContentId) &&
        ProviderContentIds.Contains(providerContentId);

    public bool IsSearchPhraseExcluded(string? phrase) =>
        !string.IsNullOrWhiteSpace(phrase) &&
        SearchPhrases.Contains(NormalizePhrase(phrase));

    public static string NormalizePhrase(string phrase) => phrase.Trim().ToLowerInvariant();
}

public sealed record BanterSelection(
    BanterContext Context,
    BanterScenario Scenario,
    string ContentType,
    string Provider,
    string? ProviderContentId,
    string? SearchPhrase,
    string? MemeTemplateId,
    string? CaptionHash,
    decimal? SelectionScore,
    string? Url);

public sealed record BanterGenerationRequest(
    BanterContext Context,
    IReadOnlyList<string?>? SuggestedQueries,
    string? Mood,
    int Seed);

public sealed record BanterGenerationResult(
    string Url,
    string MediaType,
    BanterScenario? Scenario,
    string? SearchPhrase,
    string? ProviderContentId,
    bool UsedLegacyPath,
    bool UsedFallback,
    string? FallbackReason = null);
