using BanterApp.Api.Integrations.Banter;
using BanterApp.Api.Integrations.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public class FeatureFlaggedBanterGeneratorTests
{
    [Fact]
    public async Task FlagOff_UsesLegacyPath()
    {
        var legacy = new LegacyBanterGenerator(
            new ReactionMediaResolver(
                new NullReactionGifProvider(),
                new InMemoryReactionGifLedger(),
                NullLogger<ReactionMediaResolver>.Instance));

        var orchestrator = CreateOrchestrator(withCandidates: false);
        var sut = new FeatureFlaggedBanterGenerator(
            Options.Create(new BanterOptions { UseStrategyEngine = false }),
            legacy,
            orchestrator,
            NullLogger<FeatureFlaggedBanterGenerator>.Instance);

        var result = await sut.GenerateAsync(Request());

        Assert.True(result.UsedLegacyPath);
        Assert.False(string.IsNullOrWhiteSpace(result.Url));
    }

    [Fact]
    public async Task FlagOn_UsesStrategyEnginePath()
    {
        var legacy = new LegacyBanterGenerator(
            new ReactionMediaResolver(
                new NullReactionGifProvider(),
                new InMemoryReactionGifLedger(),
                NullLogger<ReactionMediaResolver>.Instance));

        var orchestrator = CreateOrchestrator(withCandidates: true);
        var sut = new FeatureFlaggedBanterGenerator(
            Options.Create(new BanterOptions { UseStrategyEngine = true }),
            legacy,
            orchestrator,
            NullLogger<FeatureFlaggedBanterGenerator>.Instance);

        var result = await sut.GenerateAsync(Request());

        Assert.False(result.UsedLegacyPath);
        Assert.False(result.UsedFallback);
        Assert.Equal("gif", result.MediaType);
        Assert.False(string.IsNullOrWhiteSpace(result.Url));
        Assert.False(string.IsNullOrWhiteSpace(result.ProviderContentId));
        Assert.NotNull(result.Scenario);
    }

    private static BanterOrchestrator CreateOrchestrator(bool withCandidates)
    {
        var opts = Options.Create(new BanterOptions
        {
            UseStrategyEngine = true,
            ConceptsUsedPerGeneration = 2,
            CandidatesPerConcept = 5,
            ConceptCount = 4,
            TopCandidatePoolSize = 5
        });
        opts.Value.ValidateOrNormalize();

        IBanterCandidateProvider candidates = withCandidates
            ? new StubCandidateProvider(withHits: true)
            : new StubCandidateProvider(withHits: false);

        return new BanterOrchestrator(
            opts,
            new DeterministicBanterScenarioClassifier(),
            new StubConceptGenerator(),
            candidates,
            new StubHistory(),
            new BanterCandidateScorer(opts),
            new WeightedBanterCandidateSelector(opts, new SeededBanterRandom(1)),
            new SeededBanterRandom(1),
            new InMemoryReactionGifLedger(),
            new ReactionMediaResolver(
                new NullReactionGifProvider(),
                new InMemoryReactionGifLedger(),
                NullLogger<ReactionMediaResolver>.Instance),
            NullLogger<BanterOrchestrator>.Instance);
    }

    private static BanterGenerationRequest Request() =>
        BanterContextFactory.CreateRequest(
            BanterContextFactory.FromFeedItem(null, "title", "body", "news", "news"),
            ["football meme"],
            "news",
            seed: 7);

    private sealed class StubConceptGenerator : IBanterConceptGenerator
    {
        public Task<IReadOnlyList<BanterConcept>> GenerateAsync(
            BanterContext context,
            BanterScenario scenario,
            BanterExclusionContext exclusions,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BanterConcept>>([new BanterConcept("football meme")]);
    }

    private sealed class StubCandidateProvider(bool withHits) : IBanterCandidateProvider
    {
        public Task<IReadOnlyList<BanterCandidate>> GetCandidatesAsync(
            string query,
            int limit,
            CancellationToken cancellationToken = default)
        {
            if (!withHits)
            {
                return Task.FromResult<IReadOnlyList<BanterCandidate>>([]);
            }

            IReadOnlyList<BanterCandidate> hits =
            [
                new("giphy", "gif-a", BanterContentType.Gif, query, "https://media.giphy.com/media/gif-a/giphy.gif", 0, [query]),
                new("giphy", "gif-b", BanterContentType.Gif, query, "https://media.giphy.com/media/gif-b/giphy.gif", 1, [query])
            ];
            return Task.FromResult(hits);
        }
    }

    private sealed class StubHistory : IBanterHistoryService
    {
        public Task<BanterExclusionContext> GetExclusionsAsync(
            BanterContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(BanterExclusionContext.Empty);

        public Task RecordAsync(BanterSelection selection, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
