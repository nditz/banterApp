using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Integrations.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BanterApp.Api.Tests;

public class FeedReactionMediaServiceTests
{
    [Fact]
    public void IsBundledSticker_DetectsLocalReactionAssets()
    {
        Assert.True(FeedGifCatalog.IsBundledSticker("/reactions/chaos-pick.svg"));
        Assert.False(FeedGifCatalog.IsBundledSticker("https://media1.giphy.com/media/abc/giphy.gif"));
        Assert.False(FeedGifCatalog.IsBundledSticker("https://media.tenor.com/foo.gif"));
        Assert.False(FeedGifCatalog.IsBundledSticker(null));
    }

    [Fact]
    public void BuildSearchQueries_IncludesHeadlineAuthorAndSummary()
    {
        var queries = FeedReactionMediaService
            .BuildSearchQueries(
                "Brazil stun France in chaos 🔥",
                "Nobody saw that coming from the Selecao.",
                "Gary Neville",
                "pundit_quote")
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .ToList();

        Assert.Contains(queries, q => q!.Contains("Brazil", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(queries, q => q!.Contains("Gary Neville", StringComparison.OrdinalIgnoreCase));
        Assert.True(queries.Count >= 2);
    }

    [Fact]
    public async Task PresentAsync_ServesStoredLibraryUrlWithoutLiveLookup()
    {
        var provider = new RecordingGifProvider();
        var service = new FeedReactionMediaService(
            new ReactionMediaResolver(
                provider,
                new InMemoryReactionGifLedger(),
                NullLogger<ReactionMediaResolver>.Instance),
            provider);
        var item = new NewsFeedItem
        {
            Id = "feed-1",
            Title = "Receipts found",
            ImageUrl = "/reactions/receipts-found.svg",
            MediaType = "gif",
            PublishedAt = DateTimeOffset.UtcNow,
        };

        var media = await service.PresentAsync(item, "Receipts found");

        Assert.NotNull(media);
        Assert.Equal("/reactions/receipts-found.svg", media!.Url);
        Assert.Empty(provider.Queries);
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
