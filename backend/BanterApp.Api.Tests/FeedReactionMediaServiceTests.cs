using BanterApp.Api.Features.Feed;
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
}
