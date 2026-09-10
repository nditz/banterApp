using BanterApp.Api.Integrations.Media.Dtos;
using BanterApp.Api.Integrations.Pundits;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class PunditMediaItemServiceTests
{
    [Fact]
    public async Task UpsertItemAsync_reuses_unsaved_row_with_same_source_and_external_id()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new PunditMediaItemService(db);
        var source = await service.EnsureSourceAsync(
            "Sky Sports",
            "rss",
            "https://example.com/feed.xml",
            rssUrl: "https://example.com/feed.xml");

        var first = await service.UpsertItemAsync(source, Article("guid-1", "Preview A"), CancellationToken.None);
        var second = await service.UpsertItemAsync(source, Article("guid-1", "Preview B"), CancellationToken.None);

        Assert.Equal((1, 0, 0, true), first);
        Assert.Equal((0, 1, 0, false), second);
        Assert.Equal(1, db.MediaItems.Local.Count(x => x.MediaSourceId == source.Id && x.ExternalId == "guid-1"));

        await db.SaveChangesAsync();
        Assert.Equal(1, await db.MediaItems.CountAsync());
        Assert.Equal("Preview B", (await db.MediaItems.SingleAsync()).Title);
    }

    [Fact]
    public async Task UpsertItemAsync_updates_row_already_saved_in_database()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new PunditMediaItemService(db);
        var source = await service.EnsureSourceAsync(
            "Sky Sports",
            "rss",
            "https://example.com/feed.xml",
            rssUrl: "https://example.com/feed.xml");

        await service.UpsertItemAsync(source, Article("guid-1", "Preview A"), CancellationToken.None);
        db.ChangeTracker.Clear();

        var reloaded = await db.MediaSources.SingleAsync();
        var second = await service.UpsertItemAsync(reloaded, Article("guid-1", "Preview B"), CancellationToken.None);

        Assert.Equal((0, 1, 0, false), second);
        Assert.Equal(1, await db.MediaItems.CountAsync());
        Assert.Equal("Preview B", (await db.MediaItems.SingleAsync()).Title);
    }

    [Fact]
    public async Task EnsureSourceAsync_does_not_flush_duplicate_pending_items_from_earlier_feed()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new PunditMediaItemService(db);
        var firstSource = await service.EnsureSourceAsync(
            "Feed One",
            "rss",
            "https://example.com/one.xml",
            rssUrl: "https://example.com/one.xml");

        await service.UpsertItemAsync(firstSource, Article("same-guid", "First"), CancellationToken.None);
        await service.UpsertItemAsync(firstSource, Article("same-guid", "Duplicate"), CancellationToken.None);

        var secondSource = await service.EnsureSourceAsync(
            "Feed Two",
            "rss",
            "https://example.com/two.xml",
            rssUrl: "https://example.com/two.xml");

        await db.SaveChangesAsync();

        Assert.NotEqual(firstSource.Id, secondSource.Id);
        Assert.Equal(1, await db.MediaItems.CountAsync());
        Assert.Equal(2, await db.MediaSources.CountAsync());
        Assert.Equal("Duplicate", (await db.MediaItems.SingleAsync()).Title);
    }

    [Fact]
    public async Task UpsertItemAsync_skips_blank_external_id()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new PunditMediaItemService(db);
        var source = await service.EnsureSourceAsync("Feed", "rss", "https://example.com/feed.xml");

        var result = await service.UpsertItemAsync(
            source,
            Article("   ", "No guid"),
            CancellationToken.None);

        Assert.Equal((0, 0, 1, false), result);
        Assert.Empty(db.MediaItems.Local);
    }

    private static MediaItemDto Article(string externalId, string title) =>
        new(
            externalId,
            title,
            "summary",
            "https://example.com/article",
            null,
            DateTimeOffset.UtcNow,
            "https://example.com/feed.xml",
            Publication: "Sky Sports",
            FullText: "body");
}
