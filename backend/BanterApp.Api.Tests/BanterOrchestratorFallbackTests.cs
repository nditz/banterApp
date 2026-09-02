using BanterApp.Api.Integrations.Banter;
using BanterApp.Api.Integrations.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public class BanterOrchestratorFallbackTests
{
    [Fact]
    public async Task EmptyGiphyPool_FallsBackToLegacyResolver()
    {
        var opts = Options.Create(new BanterOptions
        {
            UseStrategyEngine = true,
            ConceptsUsedPerGeneration = 2,
            CandidatesPerConcept = 5,
            ConceptCount = 4
        });
        opts.Value.ValidateOrNormalize();

        var ledger = new InMemoryReactionGifLedger();
        var resolver = new ReactionMediaResolver(
            new NullReactionGifProvider(),
            ledger,
            NullLogger<ReactionMediaResolver>.Instance);

        var sut = new BanterOrchestrator(
            opts,
            new DeterministicBanterScenarioClassifier(),
            new FallingConceptGenerator(),
            new EmptyCandidateProvider(),
            new NoopHistory(),
            new BanterCandidateScorer(opts),
            new WeightedBanterCandidateSelector(opts, new SeededBanterRandom(3)),
            new SeededBanterRandom(3),
            ledger,
            resolver,
            NullLogger<BanterOrchestrator>.Instance);

        var result = await sut.GenerateAsync(
            BanterContextFactory.CreateRequest(
                BanterContextFactory.FromFeedItem(null, " Arsenal thrash Chelsea", "5-0", "match_result", "celebrate"),
                ["celebration"],
                "celebrate",
                seed: 99));

        Assert.True(result.UsedFallback);
        Assert.Equal("empty_pool", result.FallbackReason);
        Assert.False(string.IsNullOrWhiteSpace(result.Url));
        Assert.Equal(BanterScenario.GenericWin, result.Scenario);
        // Feed/job contract: MediaType is gif|image and Url is set (maps to NewsFeedItem.ImageUrl).
        Assert.True(
            result.MediaType is "gif" or "image",
            $"Unexpected media type '{result.MediaType}'");
    }

    [Fact]
    public async Task GiphyProviderThrows_FallsBackToLegacyResolver()
    {
        var opts = Options.Create(new BanterOptions
        {
            UseStrategyEngine = true,
            ConceptsUsedPerGeneration = 2,
            CandidatesPerConcept = 5,
            ConceptCount = 4
        });
        opts.Value.ValidateOrNormalize();

        var ledger = new InMemoryReactionGifLedger();
        var resolver = new ReactionMediaResolver(
            new NullReactionGifProvider(),
            ledger,
            NullLogger<ReactionMediaResolver>.Instance);

        var sut = new BanterOrchestrator(
            opts,
            new DeterministicBanterScenarioClassifier(),
            new FallingConceptGenerator(),
            new ThrowingCandidateProvider(),
            new NoopHistory(),
            new BanterCandidateScorer(opts),
            new WeightedBanterCandidateSelector(opts, new SeededBanterRandom(3)),
            new SeededBanterRandom(3),
            ledger,
            resolver,
            NullLogger<BanterOrchestrator>.Instance);

        var result = await sut.GenerateAsync(
            BanterContextFactory.CreateRequest(
                BanterContextFactory.FromFeedItem(null, "title", "body", "news", "news"),
                ["football meme"],
                "news",
                seed: 11));

        Assert.True(result.UsedFallback);
        Assert.Equal("empty_pool", result.FallbackReason);
        Assert.False(string.IsNullOrWhiteSpace(result.Url));
    }

    private sealed class FallingConceptGenerator : IBanterConceptGenerator
    {
        public Task<IReadOnlyList<BanterConcept>> GenerateAsync(
            BanterContext context,
            BanterScenario scenario,
            BanterExclusionContext exclusions,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BanterConcept>>(
                PredefinedBanterConcepts.ForScenario(scenario, 4));
    }

    private sealed class EmptyCandidateProvider : IBanterCandidateProvider
    {
        public Task<IReadOnlyList<BanterCandidate>> GetCandidatesAsync(
            string query,
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BanterCandidate>>([]);
    }

    private sealed class ThrowingCandidateProvider : IBanterCandidateProvider
    {
        public Task<IReadOnlyList<BanterCandidate>> GetCandidatesAsync(
            string query,
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("giphy unavailable");
    }

    private sealed class NoopHistory : IBanterHistoryService
    {
        public Task<BanterExclusionContext> GetExclusionsAsync(
            BanterContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(BanterExclusionContext.Empty);

        public Task RecordAsync(BanterSelection selection, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
