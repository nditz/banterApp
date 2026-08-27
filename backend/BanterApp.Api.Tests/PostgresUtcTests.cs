using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public class PostgresUtcTests
{
    [Fact]
    public void Normalize_ConvertsNonUtcOffsetToZero()
    {
        var bst = new DateTimeOffset(2026, 8, 21, 20, 0, 0, TimeSpan.FromHours(1));

        var utc = PostgresUtc.Normalize(bst);

        Assert.Equal(TimeSpan.Zero, utc.Offset);
        Assert.Equal(new DateTimeOffset(2026, 8, 21, 19, 0, 0, TimeSpan.Zero), utc);
    }

    [Fact]
    public void Normalize_LeavesUtcUnchanged()
    {
        var utc = new DateTimeOffset(2026, 8, 21, 19, 0, 0, TimeSpan.Zero);
        Assert.Equal(utc, PostgresUtc.Normalize(utc));
    }

    [Fact]
    public async Task SaveChanges_RewritesNonUtcDateTimeOffsets()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);

        var bstKickoff = new DateTimeOffset(2026, 8, 21, 20, 0, 0, TimeSpan.FromHours(1));
        var bstPublished = new DateTimeOffset(2026, 8, 27, 23, 30, 0, TimeSpan.FromHours(1));

        db.Matches.Add(new Match
        {
            Id = "pl26-mw1-utc",
            TeamA = "Arsenal",
            TeamB = "Coventry City",
            KickoffTime = bstKickoff,
            PredictionLockAtUtc = bstKickoff
        });
        db.NewsFeedItems.Add(new NewsFeedItem
        {
            Id = "news-utc",
            Source = "BBC Sport",
            Title = "Test",
            Url = "https://example.test/news",
            PublishedAt = bstPublished
        });

        await db.SaveChangesAsync();

        var match = await db.Matches.SingleAsync(m => m.Id == "pl26-mw1-utc");
        var news = await db.NewsFeedItems.SingleAsync(n => n.Id == "news-utc");

        Assert.Equal(TimeSpan.Zero, match.KickoffTime.Offset);
        Assert.Equal(TimeSpan.Zero, match.PredictionLockAtUtc!.Value.Offset);
        Assert.Equal(new DateTimeOffset(2026, 8, 21, 19, 0, 0, TimeSpan.Zero), match.KickoffTime);
        Assert.Equal(TimeSpan.Zero, news.PublishedAt.Offset);
        Assert.Equal(new DateTimeOffset(2026, 8, 27, 22, 30, 0, TimeSpan.Zero), news.PublishedAt);
    }
}
