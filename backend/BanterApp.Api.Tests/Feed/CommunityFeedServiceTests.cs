using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests.Feed;

public sealed class CommunityFeedServiceTests
{
    [Fact]
    public async Task Skips_fixtures_below_the_crowd_threshold()
    {
        await using var db = CreateDb();
        AddMatch(db, "pl26-1", DateTimeOffset.UtcNow.AddDays(2));
        AddPredictions(db, "pl26-1", "home", CommunityFeedService.MinimumPicksForCrowdCard - 1);
        await db.SaveChangesAsync();

        var items = await CommunityFeedService.BuildAsync(db, 5);

        Assert.Empty(items);
    }

    [Fact]
    public async Task Builds_a_pre_match_crowd_card_once_the_threshold_is_met()
    {
        await using var db = CreateDb();
        AddMatch(db, "pl26-2", DateTimeOffset.UtcNow.AddDays(2));
        AddPredictions(db, "pl26-2", "home", 3);
        AddPredictions(db, "pl26-2", "draw", 1);
        await db.SaveChangesAsync();

        var items = await CommunityFeedService.BuildAsync(db, 5);

        var card = Assert.Single(items);
        Assert.Equal("leaderboard", card.Type);
        Assert.Contains("75%", card.Body);
        Assert.Contains("Arsenal to win", card.Body);
    }

    [Fact]
    public async Task Reports_the_crowd_being_wrong_after_full_time()
    {
        await using var db = CreateDb();
        var match = AddMatch(db, "pl26-3", DateTimeOffset.UtcNow.AddDays(-1));
        match.Status = "FT";
        match.HomeScore = 0;
        match.AwayScore = 2;
        AddPredictions(db, "pl26-3", "home", 4);
        await db.SaveChangesAsync();

        var items = await CommunityFeedService.BuildAsync(db, 5);

        var card = Assert.Single(items);
        Assert.Equal("The crowd got cooked", card.Title);
    }

    [Fact]
    public async Task Never_emits_user_identifiers()
    {
        await using var db = CreateDb();
        AddMatch(db, "pl26-4", DateTimeOffset.UtcNow.AddDays(2));
        var userIds = AddPredictions(db, "pl26-4", "away", 4);
        await db.SaveChangesAsync();

        var items = await CommunityFeedService.BuildAsync(db, 5);

        var card = Assert.Single(items);
        var serialized = $"{card.Id} {card.Title} {card.Body} {card.Source}";
        foreach (var userId in userIds)
        {
            Assert.DoesNotContain(userId.ToString(), serialized, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static Match AddMatch(AppDbContext db, string id, DateTimeOffset kickoff)
    {
        var match = new Match
        {
            Id = id,
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            TeamACode = "ARS",
            TeamBCode = "CHE",
            KickoffTime = kickoff,
            Status = "NS",
            Group = "PL",
            Stage = "Regular Season - 1"
        };
        db.Matches.Add(match);
        return match;
    }

    private static List<Guid> AddPredictions(AppDbContext db, string matchId, string value, int count)
    {
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var userId = Guid.NewGuid();
            ids.Add(userId);
            db.Predictions.Add(new Prediction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MatchId = matchId,
                PredictionType = PredictionType.Result,
                PredictionValue = value,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return ids;
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
