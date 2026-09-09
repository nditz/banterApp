using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public class GifLibraryServiceTests
{
    [Fact]
    public async Task EnsureSeededAsync_InsertsBundledAssetsWithDescriptions()
    {
        await using var db = TestDbContextFactory.Create();
        var library = new GifLibraryService(db);

        await library.EnsureSeededAsync();

        var assets = await db.GifAssets.ToListAsync();
        Assert.Equal(GifLibraryService.BundledSeeds.Count, assets.Count);
        Assert.All(assets, a =>
        {
            Assert.StartsWith("/reactions/", a.Url);
            Assert.False(string.IsNullOrWhiteSpace(a.Description));
            Assert.Equal(GifAssetSources.Bundled, a.Source);
        });
        Assert.Contains(await db.GifSearchQueries.Select(q => q.Phrase).ToListAsync(), p => p == "premier league");
    }

    [Fact]
    public async Task PickAsync_PrefersMoodMatchAndSkipsClaimedUrls()
    {
        await using var db = TestDbContextFactory.Create();
        var library = new GifLibraryService(db);
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var first = await library.PickAsync(
            "celebrate",
            ["receipts", "celebration"],
            (id, url) =>
            {
                if (!claimed.Add(url))
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            });

        var second = await library.PickAsync(
            "celebrate",
            ["receipts"],
            (id, url) =>
            {
                if (!claimed.Add(url))
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            });

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.Url, second!.Url);
        Assert.Contains("celebrate", $"{first.Mood} {first.Tags}", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpsertQueriesAsync_StoresFootballTrendingPhrasesOnlyAsActive()
    {
        await using var db = TestDbContextFactory.Create();
        var library = new GifLibraryService(db);

        await library.UpsertQueriesAsync(
            ["happy birthday", "premier league meme", "haaland"],
            GifSearchQuerySources.GiphyTrending);

        var rows = await db.GifSearchQueries.ToListAsync();
        Assert.Contains(rows, q => q.Phrase == "premier league meme" && q.IsFootballRelated && q.IsActive);
        Assert.Contains(rows, q => q.Phrase == "haaland" && q.IsFootballRelated);
        var birthday = rows.Single(q => q.Phrase == "happy birthday");
        Assert.False(birthday.IsFootballRelated);
        Assert.False(birthday.IsActive);
    }
}
