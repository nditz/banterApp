namespace BanterApp.Api.Integrations.Banter;

public interface IBanterGenerator
{
    Task<BanterGenerationResult> GenerateAsync(
        BanterGenerationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IBanterScenarioClassifier
{
    Task<BanterScenario> ClassifyAsync(BanterContext context, CancellationToken cancellationToken = default);
}

public interface IBanterConceptGenerator
{
    Task<IReadOnlyList<BanterConcept>> GenerateAsync(
        BanterContext context,
        BanterScenario scenario,
        BanterExclusionContext exclusions,
        CancellationToken cancellationToken = default);
}

public interface IBanterCandidateProvider
{
    Task<IReadOnlyList<BanterCandidate>> GetCandidatesAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default);
}

public interface IBanterHistoryService
{
    Task<BanterExclusionContext> GetExclusionsAsync(
        BanterContext context,
        CancellationToken cancellationToken = default);

    Task RecordAsync(BanterSelection selection, CancellationToken cancellationToken = default);
}

public interface IBanterCandidateScorer
{
    IReadOnlyList<ScoredBanterCandidate> Score(
        BanterContext context,
        IEnumerable<BanterCandidate> candidates,
        BanterExclusionContext exclusions);
}

public interface IBanterCandidateSelector
{
    ScoredBanterCandidate? Select(IReadOnlyList<ScoredBanterCandidate> candidates);
}

/// <summary>Injectable RNG for deterministic tests of weighted selection.</summary>
public interface IBanterRandom
{
    double NextDouble();
    int Next(int maxExclusive);
}
