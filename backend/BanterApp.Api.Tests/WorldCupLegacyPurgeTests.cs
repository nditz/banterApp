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
}
