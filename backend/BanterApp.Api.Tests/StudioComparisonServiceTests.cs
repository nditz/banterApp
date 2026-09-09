using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Opinions;
using BanterApp.Api.Features.Studio;
using BanterApp.Api.Services;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class StudioComparisonServiceTests
{
    [Fact]
    public async Task BuildAsync_includes_user_pick_and_sourced_pundit_with_attribution()
    {
        await using var db = TestDbContextFactory.Create();
        var (user, match) = await SeedUserMatchAsync(db, status: "NS");
        var pundit = await SeedPunditPredictionAsync(db, match.Id, "HOME");
        var service = CreateService(db);

        var result = await service.BuildAsync(user, matchIdsCsv: null, CancellationToken.None);

        var row = Assert.Single(result.Matches);
        Assert.Null(row.ActualResult);
        Assert.Contains(row.Picks, p => p.Role == "me" && p.WasCorrect is null);
        var punditPick = Assert.Single(row.Picks, p => p.Role == "pundit");
        Assert.Equal(pundit.Name, punditPick.Name);
        Assert.False(punditPick.IsFictionalPersona);
        Assert.Equal("https://sky.example/take", punditPick.SourceUrl);
        Assert.False(string.IsNullOrWhiteSpace(punditPick.AttributionNote));
        Assert.False(result.FilteringToFollows);
    }

    [Fact]
    public async Task BuildAsync_filters_to_followed_pundits_when_user_follows()
    {
        await using var db = TestDbContextFactory.Create();
        var (user, match) = await SeedUserMatchAsync(db, status: "NS");
        var followed = await SeedPunditPredictionAsync(db, match.Id, "HOME", name: "Followed Voice");
        await SeedPunditPredictionAsync(db, match.Id, "AWAY", name: "Other Voice");
        var follows = new PunditFollowService(db);
        await follows.FollowAsync(followed.Id, user, CancellationToken.None);
        var service = CreateService(db);

        var result = await service.BuildAsync(user, null, CancellationToken.None);

        Assert.True(result.FilteringToFollows);
        Assert.Equal(1, result.FollowedPunditCount);
        var punditPicks = result.Matches[0].Picks.Where(p => p.Role == "pundit").ToList();
        Assert.Single(punditPicks);
        Assert.Equal("Followed Voice", punditPicks[0].Name);
    }

    [Fact]
    public async Task BuildAsync_hides_rejected_or_unreviewed_pundit_predictions()
    {
        await using var db = TestDbContextFactory.Create();
        var (user, match) = await SeedUserMatchAsync(db, status: "NS");
        var pundit = await SeedPunditPredictionAsync(db, match.Id, "HOME");
        db.MediaSources.Add(new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "BBC",
            SourceType = "rss",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var sourceId = db.MediaSources.Local.First().Id;
        var item = new MediaItem
        {
            Id = Guid.NewGuid(),
            MediaSourceId = sourceId,
            ExternalId = "x",
            Title = "Take",
            SourceUrl = "https://example.com",
            ProcessingStatus = MediaItemProcessingStatus.Extracted
        };
        db.MediaItems.Add(item);
        db.PunditOpinions.Add(new PunditOpinion
        {
            Id = Guid.NewGuid(),
            SourceItemId = item.Id,
            PunditId = pundit.Id,
            MatchId = match.Id,
            Opinion = "hidden",
            Prediction = "HOME",
            NeedsHumanReview = true,
            ReviewStatus = "pending",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.BuildAsync(user, null, CancellationToken.None);

        Assert.DoesNotContain(result.Matches[0].Picks, p => p.Role == "pundit");
    }

    [Fact]
    public async Task BuildAsync_matchIds_returns_pundits_without_user_pick_and_marks_after_result()
    {
        await using var db = TestDbContextFactory.Create();
        var match = new Match
        {
            Id = "fd-mw-1",
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            TeamACode = "ARS",
            TeamBCode = "CHE",
            KickoffTime = DateTimeOffset.UtcNow.AddHours(-3),
            Status = "FT",
            HomeScore = 2,
            AwayScore = 0,
            Stage = "League",
            Venue = "Emirates"
        };
        db.Matches.Add(match);
        await SeedPunditPredictionAsync(db, match.Id, "Arsenal to win");
        var user = new UserContext { AnonymousUserId = Guid.NewGuid() };
        var service = CreateService(db);

        var result = await service.BuildAsync(user, match.Id, CancellationToken.None);

        var row = Assert.Single(result.Matches);
        Assert.Equal("2-0", row.ActualResult);
        var punditPick = Assert.Single(row.Picks, p => p.Role == "pundit");
        Assert.True(punditPick.WasCorrect);
        Assert.DoesNotContain(row.Picks, p => p.Role == "me");
    }

    [Fact]
    public async Task BuildAsync_guest_without_session_returns_pundits_for_matchIds()
    {
        await using var db = TestDbContextFactory.Create();
        var match = new Match
        {
            Id = "fd-guest-cmp",
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            TeamACode = "ARS",
            TeamBCode = "CHE",
            KickoffTime = DateTimeOffset.UtcNow.AddHours(2),
            Status = "NS",
            Stage = "League",
            Venue = "Emirates"
        };
        db.Matches.Add(match);
        await SeedPunditPredictionAsync(db, match.Id, "HOME");
        var service = CreateService(db);

        var result = await service.BuildAsync(new UserContext(), match.Id, CancellationToken.None);

        var row = Assert.Single(result.Matches);
        Assert.Contains(row.Picks, p => p.Role == "pundit");
        Assert.DoesNotContain(row.Picks, p => p.Role == "me");
    }

    [Fact]
    public async Task FormatPrediction_does_not_invent_quotes()
    {
        Assert.Equal("Home Win", StudioComparisonService.FormatPrediction("HOME", "result"));
        Assert.Equal("The Magpies sneak it", StudioComparisonService.FormatPrediction("The Magpies sneak it", "result"));
    }

    private static StudioComparisonService CreateService(AppDbContext db)
    {
        var config = new ConfigurationBuilder().Build();
        return new StudioComparisonService(
            db,
            new TournamentBonusScoringService(config),
            new PunditFollowService(db));
    }

    private static async Task<(UserContext User, Match Match)> SeedUserMatchAsync(
        AppDbContext db,
        string status)
    {
        var userId = Guid.NewGuid();
        var match = new Match
        {
            Id = "fd-mw-cmp",
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            TeamACode = "ARS",
            TeamBCode = "CHE",
            KickoffTime = DateTimeOffset.UtcNow.AddDays(1),
            Status = status,
            Stage = "League",
            Venue = "Emirates"
        };
        db.Matches.Add(match);
        db.Predictions.Add(new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchId = match.Id,
            PredictionType = PredictionType.Result,
            PredictionValue = "home",
            PointsAwarded = 0,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return (new UserContext { UserId = userId }, match);
    }

    private static async Task<Pundit> SeedPunditPredictionAsync(
        AppDbContext db,
        string matchId,
        string prediction,
        string name = "Gary Neville")
    {
        var pundit = new Pundit
        {
            Id = Guid.NewGuid(),
            Kind = PunditKind.Source,
            Name = name,
            NormalizedName = name.ToLowerInvariant(),
            Organization = "Sky Sports",
            AttributionMode = PunditAttributionMode.Licensed,
            SourceUrl = "https://sky.example/take"
        };
        db.Pundits.Add(pundit);
        db.PunditPredictions.Add(new PunditPrediction
        {
            Id = Guid.NewGuid(),
            PunditId = pundit.Id,
            MatchId = matchId,
            Prediction = prediction,
            SourceUrl = "https://sky.example/take",
            SourceType = "article",
            IsMatched = true,
            PublishedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return pundit;
    }
}
