using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public class WorldCupLegacyPurgeTests
{
    [Fact]
    public async Task RemovesOpenFootballFixturesAndWorldCupNews()
    {
        await using var db = TestDbContextFactory.Create();
        db.Matches.AddRange(
            new Match
            {
                Id = "of26-ko-1",
                TeamA = "Mexico",
                TeamB = "South Africa",
                TeamACode = "MEX",
                TeamBCode = "RSA",
                KickoffTime = DateTimeOffset.UtcNow.AddDays(-10),
                Stage = "Round of 32",
                Group = "A",
                Venue = "Mexico City",
                Status = "FT",
                HomeScore = 1,
                AwayScore = 0
            },
            new Match
            {
                Id = "pl26-mw1-1",
                TeamA = "Arsenal",
                TeamB = "Coventry City",
                TeamACode = "ARS",
                TeamBCode = "COV",
                KickoffTime = DateTimeOffset.UtcNow.AddDays(-6),
                Stage = "Premier League",
                Group = "PL",
                Venue = "Emirates Stadium",
                Status = "FT",
                HomeScore = 3,
                AwayScore = 0,
                MatchweekNumber = 1
            });
        db.NewsFeedItems.Add(new NewsFeedItem
        {
            Id = "news-wc",
            Source = "BBC",
            Title = "World Cup round of 32 recap",
            Url = "https://www.bbc.co.uk/sport/football/world-cup/example",
            PublishedAt = DateTimeOffset.UtcNow.AddHours(-2)
        });
        db.StandingRows.Add(new StandingRow
        {
            Id = Guid.NewGuid(),
            GroupKey = "A",
            Rank = 1,
            TeamCode = "MEX",
            TeamName = "Mexico",
            Played = 3,
            Points = 9
        });
        db.Players.Add(new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = "National leftover",
            IsActive = true,
            ClubName = null
        });
        await db.SaveChangesAsync();

        var removed = await WorldCupLegacyPurge.ExecuteAsync(db);

        Assert.Equal(1, removed);
        Assert.False(await db.Matches.AnyAsync(m => m.Id.StartsWith("of26-")));
        Assert.True(await db.Matches.AnyAsync(m => m.Id == "pl26-mw1-1"));
        Assert.False(await db.NewsFeedItems.AnyAsync());
        Assert.False(await db.StandingRows.AnyAsync(s => s.GroupKey != "PL"));
        Assert.False((await db.Players.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task RemovesMisStampedApiFootballWorldCupRows()
    {
        await using var db = TestDbContextFactory.Create();
        db.Matches.AddRange(
            new Match
            {
                Id = "apifb-999",
                TeamA = "England",
                TeamB = "Brazil",
                TeamACode = "ENG",
                TeamBCode = "BRA",
                KickoffTime = DateTimeOffset.UtcNow.AddDays(-2),
                Stage = "Group A - 1",
                Group = "A",
                Venue = "MetLife",
                Status = "FT",
                HomeScore = 1,
                AwayScore = 0,
                CompetitionSeasonId = PremierLeagueCatalog.SeasonId
            },
            new Match
            {
                Id = "apifb-bare",
                TeamA = "Some Club",
                TeamB = "Other Club",
                TeamACode = "SOM",
                TeamBCode = "OTH",
                KickoffTime = DateTimeOffset.UtcNow.AddDays(1),
                Stage = "Unknown",
                Group = "",
                Venue = "Somewhere",
                Status = "NS"
            },
            new Match
            {
                Id = "pl26-mw1-1",
                TeamA = "Arsenal",
                TeamB = "Coventry City",
                TeamACode = "ARS",
                TeamBCode = "COV",
                KickoffTime = DateTimeOffset.UtcNow.AddDays(-6),
                Stage = "Premier League",
                Group = "PL",
                Venue = "Emirates Stadium",
                Status = "FT",
                HomeScore = 3,
                AwayScore = 0,
                MatchweekNumber = 1,
                CompetitionSeasonId = PremierLeagueCatalog.SeasonId
            });
        await db.SaveChangesAsync();

        var removed = await WorldCupLegacyPurge.ExecuteAsync(db);

        Assert.Equal(2, removed);
        Assert.True(await db.Matches.AnyAsync(m => m.Id == "pl26-mw1-1"));
        Assert.False(await db.Matches.AnyAsync(m => m.Id.StartsWith("apifb-")));
    }

    [Fact]
    public async Task RemovesWorldCupMediaItemsAndDeactivatesGifQueries()
    {
        await using var db = TestDbContextFactory.Create();
        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "Sky Sports",
            SourceType = "rss",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var fifaSource = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "FIFA World Cup",
            SourceType = "rss",
            SiteUrl = "https://www.fifa.com/tournaments/mens/worldcup",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.MediaSources.AddRange(source, fifaSource);
        db.MediaItems.AddRange(
            new MediaItem
            {
                Id = Guid.NewGuid(),
                MediaSourceId = source.Id,
                ExternalId = "pl-1",
                Title = "Arsenal vs Chelsea preview",
                SourceUrl = "https://www.skysports.com/football/arsenal-chelsea",
                Description = "Premier League preview",
                ProcessingStatus = MediaItemProcessingStatus.Pending,
                LastSyncedAt = DateTimeOffset.UtcNow
            },
            new MediaItem
            {
                Id = Guid.NewGuid(),
                MediaSourceId = source.Id,
                ExternalId = "wc-1",
                Title = "World Cup hydration breaks",
                SourceUrl = "https://www.theguardian.com/football/world-cup/2026",
                Description = "World Cup notes",
                ProcessingStatus = MediaItemProcessingStatus.Pending,
                LastSyncedAt = DateTimeOffset.UtcNow
            });
        db.GifSearchQueries.Add(new GifSearchQuery
        {
            Id = Guid.NewGuid(),
            Phrase = "world cup celebration",
            Source = GifSearchQuerySources.GiphyTrending,
            IsFootballRelated = true,
            IsActive = true
        });
        db.GeneratedContents.Add(new GeneratedContent
        {
            Id = Guid.NewGuid(),
            Type = GeneratedContentType.Banter,
            Prompt = "Write World Cup banter",
            Output = "England to the semi-finals"
        });
        await db.SaveChangesAsync();

        var removed = await WorldCupLegacyPurge.ExecuteAsync(db);

        Assert.Equal(0, removed);
        Assert.Equal("Arsenal vs Chelsea preview", (await db.MediaItems.SingleAsync()).Title);
        Assert.False(await db.GeneratedContents.AnyAsync());
        Assert.False((await db.GifSearchQueries.SingleAsync()).IsActive);
        Assert.True((await db.MediaSources.SingleAsync(s => s.Name == "Sky Sports")).IsActive);
        Assert.False((await db.MediaSources.SingleAsync(s => s.Name == "FIFA World Cup")).IsActive);
    }
}
