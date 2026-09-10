using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Aura;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests.Aura;

public sealed class AuraSummaryTests
{
    [Fact]
    public async Task Total_is_awarded_points_plus_matchweek_bonuses()
    {
        await using var db = CreateDb();
        var user = AddUser(db);
        AddSettledPrediction(db, user.Id, "pl26-1", points: 3, kickoff: DateTimeOffset.UtcNow.AddDays(-2));
        AddSettledPrediction(db, user.Id, "pl26-2", points: 1, kickoff: DateTimeOffset.UtcNow.AddDays(-3));
        db.MatchweekBonuses.Add(new MatchweekBonus
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            MatchweekNumber = 1,
            PointsAwarded = 5
        });
        await db.SaveChangesAsync();

        var summary = await AuraEndpoints.BuildAsync(db, Context(user.Id), default);

        Assert.Equal(9, summary.Total);
        Assert.Equal(2, summary.SettledPicks);
        Assert.Equal(2, summary.CorrectPicks);
    }

    [Fact]
    public async Task Weekly_change_only_counts_the_last_seven_days()
    {
        await using var db = CreateDb();
        var user = AddUser(db);
        AddSettledPrediction(db, user.Id, "pl26-1", points: 3, kickoff: DateTimeOffset.UtcNow.AddDays(-1));
        AddSettledPrediction(db, user.Id, "pl26-2", points: 3, kickoff: DateTimeOffset.UtcNow.AddDays(-30));
        await db.SaveChangesAsync();

        var summary = await AuraEndpoints.BuildAsync(db, Context(user.Id), default);

        Assert.Equal(6, summary.Total);
        Assert.Equal(3, summary.WeeklyChange);
    }

    [Fact]
    public async Task Streak_stops_at_the_most_recent_blank()
    {
        await using var db = CreateDb();
        var user = AddUser(db);
        AddSettledPrediction(db, user.Id, "pl26-1", points: 3, kickoff: DateTimeOffset.UtcNow.AddDays(-1));
        AddSettledPrediction(db, user.Id, "pl26-2", points: 1, kickoff: DateTimeOffset.UtcNow.AddDays(-2));
        AddSettledPrediction(db, user.Id, "pl26-3", points: 0, kickoff: DateTimeOffset.UtcNow.AddDays(-3));
        AddSettledPrediction(db, user.Id, "pl26-4", points: 3, kickoff: DateTimeOffset.UtcNow.AddDays(-4));
        await db.SaveChangesAsync();

        var summary = await AuraEndpoints.BuildAsync(db, Context(user.Id), default);

        Assert.Equal(2, summary.Streak);
        Assert.Equal(4, summary.SettledPicks);
        Assert.Equal(3, summary.CorrectPicks);
    }

    [Fact]
    public async Task Unplayed_fixtures_do_not_count_as_settled()
    {
        await using var db = CreateDb();
        var user = AddUser(db);
        var match = AddMatch(db, "pl26-1", DateTimeOffset.UtcNow.AddDays(3));
        match.Status = "NS";
        db.Predictions.Add(new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            MatchId = match.Id,
            PredictionType = PredictionType.Result,
            PredictionValue = "home",
            PointsAwarded = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var summary = await AuraEndpoints.BuildAsync(db, Context(user.Id), default);

        Assert.Equal(0, summary.SettledPicks);
        Assert.Equal(0, summary.Streak);
    }

    [Fact]
    public async Task Rank_places_the_user_against_everyone_who_has_picked()
    {
        await using var db = CreateDb();
        var user = AddUser(db);
        var rival = AddUser(db);
        var trailer = AddUser(db);
        AddSettledPrediction(db, user.Id, "pl26-1", points: 3, kickoff: DateTimeOffset.UtcNow.AddDays(-1));
        AddSettledPrediction(db, rival.Id, "pl26-2", points: 9, kickoff: DateTimeOffset.UtcNow.AddDays(-1));
        AddSettledPrediction(db, trailer.Id, "pl26-3", points: 1, kickoff: DateTimeOffset.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();

        var summary = await AuraEndpoints.BuildAsync(db, Context(user.Id), default);

        Assert.Equal(2, summary.Rank);
        Assert.Equal(3, summary.TotalPlayers);
        Assert.Equal(50, summary.Percentile);
    }

    private static IUserContext Context(Guid userId) => new UserContext { UserId = userId };

    private static User AddUser(AppDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.test",
            DisplayName = "Tester"
        };
        db.Users.Add(user);
        return user;
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

    private static void AddSettledPrediction(
        AppDbContext db,
        Guid userId,
        string matchId,
        int points,
        DateTimeOffset kickoff)
    {
        var match = db.Matches.Local.FirstOrDefault(m => m.Id == matchId) ?? AddMatch(db, matchId, kickoff);
        match.KickoffTime = kickoff;
        match.Status = "FT";
        match.HomeScore = 2;
        match.AwayScore = 1;

        db.Predictions.Add(new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchId = matchId,
            PredictionType = PredictionType.Result,
            PredictionValue = "home",
            PointsAwarded = points,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
