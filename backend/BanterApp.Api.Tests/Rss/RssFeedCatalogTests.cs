using System.Net;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Rss;
using BanterApp.Api.Services;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BanterApp.Api.Tests.Rss;

public class ApplePodcastLookupTests
{
    [Fact]
    public void ParseFeedUrl_ReadsFirstResult()
    {
        const string json = """
            {"resultCount":1,"results":[{"collectionName":"The Rest Is Football","feedUrl":"https://feeds.megaphone.fm/GLT8847082992"}]}
            """;

        var url = ApplePodcastLookup.ParseFeedUrl(json);

        Assert.Equal("https://feeds.megaphone.fm/GLT8847082992", url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("""{"resultCount":0,"results":[]}""")]
    [InlineData("""{"results":[{"collectionName":"x"}]}""")]
    public void ParseFeedUrl_Invalid_ReturnsNull(string? json)
    {
        Assert.Null(ApplePodcastLookup.ParseFeedUrl(json));
    }
}

public class RssFeedCatalogTests
{
    [Fact]
    public async Task Seed_DoesNotOverwriteResolvedUrl()
    {
        await using var db = TestDbContextFactory.Create();
        var catalog = CreateCatalog(
            db,
            new RssFeedSeedEntry
            {
                Name = "The Rest Is Football",
                Kind = RssFeedKind.Podcast,
                RssUrl = "https://old.example/rss",
                ApplePodcastId = 1701022490,
                SourceWeight = 1.2,
                ExtractPredictions = true,
                UseForMediaIngest = true
            });

        await catalog.SeedAsync();

        var feed = Assert.Single(db.RssFeeds);
        feed.RssUrl = "https://feeds.megaphone.fm/updated";
        await db.SaveChangesAsync();

        await catalog.SeedAsync();

        var reloaded = Assert.Single(db.RssFeeds);
        Assert.Equal("https://feeds.megaphone.fm/updated", reloaded.RssUrl);
        Assert.Equal(1701022490, reloaded.ApplePodcastId);
        Assert.Equal(120, reloaded.Priority);
        Assert.True(reloaded.UseForMediaIngest);
        Assert.False(reloaded.UseForNews);
    }

    [Fact]
    public async Task Seed_AppliesChannelFlagsFromSeedEntry()
    {
        await using var db = TestDbContextFactory.Create();
        var catalog = CreateCatalog(
            db,
            new RssFeedSeedEntry
            {
                Name = "BBC Sport Premier League",
                Kind = RssFeedKind.Website,
                RssUrl = "https://feeds.bbci.co.uk/sport/football/premier-league/rss.xml",
                SourceWeight = 1.0,
                UseForMediaIngest = true,
                UseForNews = true,
                UseForPundit = true
            });

        await catalog.SeedAsync();

        var feed = Assert.Single(db.RssFeeds);
        Assert.True(feed.UseForMediaIngest);
        Assert.True(feed.UseForNews);
        Assert.True(feed.UseForPundit);
        Assert.Equal("website-bbc-sport-premier-league", feed.Slug);
    }

    [Fact]
    public async Task GetActiveForNews_OrdersByPriorityDescending()
    {
        await using var db = TestDbContextFactory.Create();
        db.RssFeeds.AddRange(
            new RssFeed
            {
                Id = Guid.NewGuid(),
                Slug = "website-low",
                Name = "Low",
                Kind = RssFeedKind.Website,
                RssUrl = "https://low.example/rss",
                Priority = 50,
                UseForNews = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new RssFeed
            {
                Id = Guid.NewGuid(),
                Slug = "website-high",
                Name = "High",
                Kind = RssFeedKind.Website,
                RssUrl = "https://high.example/rss",
                Priority = 120,
                UseForNews = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new RssFeed
            {
                Id = Guid.NewGuid(),
                Slug = "website-off",
                Name = "Off",
                Kind = RssFeedKind.Website,
                RssUrl = "https://off.example/rss",
                Priority = 200,
                UseForNews = true,
                IsActive = false,
                CreatedAt = DateTimeOffset.UtcNow
            });
        await db.SaveChangesAsync();

        var catalog = CreateCatalog(db);
        var feeds = await catalog.GetActiveForNewsAsync();

        Assert.Equal(["High", "Low"], feeds.Select(f => f.Name).ToArray());
    }

    private static RssFeedCatalogService CreateCatalog(
        Data.AppDbContext db,
        params RssFeedSeedEntry[] feeds) =>
        new(db, new StaticRssFeedCatalogSeed(feeds));
}

public class RssFeedResolverTests
{
    [Fact]
    public async Task Resolve_UpdatesUrlFromAppleLookup()
    {
        await using var db = TestDbContextFactory.Create();
        db.RssFeeds.Add(new RssFeed
        {
            Id = Guid.NewGuid(),
            Slug = "podcast-rest",
            Name = "The Rest Is Football",
            Kind = RssFeedKind.Podcast,
            RssUrl = "https://old.example/rss",
            ApplePodcastId = 1701022490,
            Priority = 120,
            IsActive = true,
            UseForMediaIngest = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var http = new StubSafeHttpClient();
        http.Responses[ApplePodcastLookup.LookupUrl(1701022490)] = new SafeHttpResponse(
            """{"results":[{"feedUrl":"https://feeds.megaphone.fm/GLT8847082992"}]}""",
            "application/json",
            HttpStatusCode.OK);
        http.Responses["https://feeds.megaphone.fm/GLT8847082992"] = new SafeHttpResponse(
            "<rss><channel><title>ok</title></channel></rss>",
            "application/rss+xml",
            HttpStatusCode.OK,
            "https://feeds.megaphone.fm/GLT8847082992");

        var resolver = new RssFeedResolver(db, http, NullLogger<RssFeedResolver>.Instance);
        var result = await resolver.ResolveAsync();

        var feed = Assert.Single(db.RssFeeds);
        Assert.Equal("https://feeds.megaphone.fm/GLT8847082992", feed.RssUrl);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, feed.ConsecutiveFailures);
        Assert.True(feed.IsActive);
    }

    [Fact]
    public async Task Resolve_DeactivatesOnGone()
    {
        await using var db = TestDbContextFactory.Create();
        db.RssFeeds.Add(ActiveWebsite("https://gone.example/rss"));
        await db.SaveChangesAsync();

        var http = new StubSafeHttpClient();
        http.Responses["https://gone.example/rss"] = new SafeHttpResponse(
            string.Empty, "text/plain", HttpStatusCode.Gone, "https://gone.example/rss");

        var result = await new RssFeedResolver(db, http, NullLogger<RssFeedResolver>.Instance).ResolveAsync();

        Assert.False(Assert.Single(db.RssFeeds).IsActive);
        Assert.Equal(1, result.Deactivated);
    }

    [Fact]
    public async Task Resolve_DeactivatesAfterThreeNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        db.RssFeeds.Add(ActiveWebsite("https://missing.example/rss"));
        await db.SaveChangesAsync();

        var http = new StubSafeHttpClient();
        http.Responses["https://missing.example/rss"] = new SafeHttpResponse(
            string.Empty, "text/plain", HttpStatusCode.NotFound, "https://missing.example/rss");
        var resolver = new RssFeedResolver(db, http, NullLogger<RssFeedResolver>.Instance);

        await resolver.ResolveAsync();
        await resolver.ResolveAsync();
        Assert.True(Assert.Single(db.RssFeeds).IsActive);

        await resolver.ResolveAsync();
        var feed = Assert.Single(db.RssFeeds);
        Assert.False(feed.IsActive);
        Assert.Equal(3, feed.ConsecutiveFailures);
    }

    [Fact]
    public async Task Resolve_DoesNotDeactivateOnHtmlBody()
    {
        await using var db = TestDbContextFactory.Create();
        db.RssFeeds.Add(ActiveWebsite("https://html.example/rss"));
        await db.SaveChangesAsync();

        var http = new StubSafeHttpClient();
        http.Responses["https://html.example/rss"] = new SafeHttpResponse(
            "<html><body>not a feed</body></html>",
            "text/html",
            HttpStatusCode.OK,
            "https://html.example/rss");

        await new RssFeedResolver(db, http, NullLogger<RssFeedResolver>.Instance).ResolveAsync();

        var feed = Assert.Single(db.RssFeeds);
        Assert.True(feed.IsActive);
        Assert.Equal(0, feed.ConsecutiveFailures);
    }

    private static RssFeed ActiveWebsite(string url) => new()
    {
        Id = Guid.NewGuid(),
        Slug = "website-test",
        Name = "Test",
        Kind = RssFeedKind.Website,
        RssUrl = url,
        Priority = 100,
        IsActive = true,
        UseForNews = true,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private sealed class StubSafeHttpClient : ISafeHttpClient
    {
        public Dictionary<string, SafeHttpResponse?> Responses { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<SafeHttpResponse?> GetStringAsync(string url, CancellationToken ct = default)
        {
            Responses.TryGetValue(url, out var response);
            return Task.FromResult(response);
        }
    }
}
