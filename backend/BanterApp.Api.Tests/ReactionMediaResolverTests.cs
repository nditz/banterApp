using BanterApp.Api.Features.Feed;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public class ReactionMediaResolverTests
{
    [Fact]
    public async Task ResolveAsync_DifferentSeedsTryDifferentQueriesFirst()
    {
        var provider = new RecordingGifProvider();
        var resolver = new ReactionMediaResolver(
            provider,
            new InMemoryReactionGifLedger(),
            NullLogger<ReactionMediaResolver>.Instance);
        var phrases = new[] { "celebration", "shocked face", "crowd hype" };

        await resolver.ResolveAsync(phrases, "celebrate", seed: 0);
        var firstSeedQueries = provider.Queries.ToList();
        provider.Queries.Clear();

        await resolver.ResolveAsync(phrases, "celebrate", seed: 1);
        var secondSeedQueries = provider.Queries.ToList();

        Assert.NotEmpty(firstSeedQueries);
        Assert.NotEmpty(secondSeedQueries);
        Assert.NotEqual(firstSeedQueries[0], secondSeedQueries[0]);
    }

    [Fact]
    public async Task ResolveAsync_StickerFallbackAssignsUniqueMemesPerCard()
    {
        var resolver = new ReactionMediaResolver(
            new NullReactionGifProvider(),
            new InMemoryReactionGifLedger(),
            NullLogger<ReactionMediaResolver>.Instance);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var seed = 0; seed < FeedGifCatalog.AllDistinctUrls().Count; seed++)
        {
            var media = await resolver.ResolveAsync(null, "roast", seed);
            Assert.True(FeedGifCatalog.IsBundledSticker(media.Url));
            Assert.True(seen.Add(media.Url), $"Repeated meme/sticker {media.Url} for seed {seed}");
        }
    }

    [Fact]
    public async Task ResolveAsync_LocalLibraryBeatsLiveProviderWhenMixChanceIsZero()
    {
        await using var db = TestDbContextFactory.Create();
        var provider = new RecordingGifProvider();
        var resolver = new ReactionMediaResolver(
            provider,
            new InMemoryReactionGifLedger(),
            NullLogger<ReactionMediaResolver>.Instance,
            new GifLibraryService(db),
            gifOptions: Options.Create(new ReactionGifOptions
            {
                PreferLocalLibrary = true,
                LiveMixChance = 0,
                MaxLiveLookupsPerResolve = 1,
            }));

        var media = await resolver.ResolveAsync(["celebration"], "celebrate", seed: 42);

        Assert.True(FeedGifCatalog.IsBundledSticker(media.Url));
        Assert.Empty(provider.Queries);
    }

    [Fact]
    public void ResolveAlternate_CanLeaveTheMoodPool()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/reactions/brave-but-wrong.svg",
            "/reactions/prediction-fraud.svg",
            "/reactions/against-grain.svg",
        };

        var alternate = FeedGifCatalog.ResolveAlternate("/reactions/brave-but-wrong.svg", used);

        Assert.DoesNotContain(alternate, used);
        Assert.StartsWith("/reactions/", alternate);
    }

    private sealed class RecordingGifProvider : IReactionGifProvider
    {
        public bool IsEnabled => true;

        public List<string> Queries { get; } = [];

        public Task<string?> FindGifUrlAsync(
            string query,
            int seed,
            CancellationToken cancellationToken = default)
        {
            Queries.Add(query);
            return Task.FromResult<string?>("https://media1.giphy.com/media/x/giphy.gif");
        }
    }
}
