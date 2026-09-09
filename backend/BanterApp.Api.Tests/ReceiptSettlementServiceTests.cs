using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Receipts;
using BanterApp.Api.Services;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class ReceiptSettlementServiceTests
{
    [Fact]
    public async Task Emit_creates_private_receipt_and_is_idempotent_for_same_result()
    {
        await using var db = TestDbContextFactory.Create();
        var (prediction, match) = await SeedFinishedPredictionAsync(db, "home", points: 3);
        await SeedPunditAsync(db, match.Id, "AWAY");
        var settlement = new ReceiptSettlementService(db);

        var first = await settlement.EmitForPredictionsAsync([prediction], CancellationToken.None);
        await db.SaveChangesAsync();
        var second = await settlement.EmitForPredictionsAsync([prediction], CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var receipt = Assert.Single(await db.PredictionReceipts.Include(r => r.StoryCandidates).ToListAsync());
        Assert.False(receipt.IsPublic);
        Assert.False(ReceiptPrivacy.IsEligibleForPublicTimeline(receipt));
        Assert.Equal(ReceiptStoryTypes.BeatPundit, receipt.StoryType);
        Assert.Contains(receipt.StoryCandidates, c => c.StoryType == ReceiptStoryTypes.BeatPundit);
        Assert.DoesNotContain("\"userId\"", receipt.PunditTakesJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Emit_new_result_version_creates_second_receipt()
    {
        await using var db = TestDbContextFactory.Create();
        var (prediction, match) = await SeedFinishedPredictionAsync(db, "home", points: 3);
        var settlement = new ReceiptSettlementService(db);

        await settlement.EmitForPredictionsAsync([prediction], CancellationToken.None);
        await db.SaveChangesAsync();

        match.HomeScore = 1;
        match.AwayScore = 1;
        prediction.PointsAwarded = 0;
        await db.SaveChangesAsync();

        var created = await settlement.EmitForPredictionsAsync([prediction], CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(1, created);
        Assert.Equal(2, await db.PredictionReceipts.CountAsync());
    }

    [Fact]
    public async Task Rescore_emits_receipt_even_when_points_already_settled()
    {
        await using var db = TestDbContextFactory.Create();
        var (prediction, _) = await SeedFinishedPredictionAsync(db, "home", points: 3);
        var rescore = new PredictionRescoreService(db, new ScoringService(), new ReceiptSettlementService(db));

        var changed = await rescore.RescoreFinishedMatchesAsync(CancellationToken.None);

        Assert.Equal(0, changed);
        Assert.Equal(1, await db.PredictionReceipts.CountAsync());
        Assert.Equal(prediction.Id, (await db.PredictionReceipts.SingleAsync()).PredictionId);
    }

    [Fact]
    public async Task List_and_get_are_owner_scoped()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var (prediction, _) = await SeedFinishedPredictionAsync(db, "home", points: 3, userId: ownerId);
        var settlement = new ReceiptSettlementService(db);
        await settlement.EmitForPredictionsAsync([prediction], CancellationToken.None);
        await db.SaveChangesAsync();
        var receipt = await db.PredictionReceipts.SingleAsync();
        var queries = new ReceiptQueryService(db);

        var owner = new UserContext { UserId = ownerId };
        var other = new UserContext { UserId = otherId };

        Assert.Single(await queries.ListForUserAsync(owner, CancellationToken.None));
        Assert.Empty(await queries.ListForUserAsync(other, CancellationToken.None));
        Assert.NotNull(await queries.GetOwnedAsync(receipt.Id, owner, CancellationToken.None));
        Assert.Null(await queries.GetOwnedAsync(receipt.Id, other, CancellationToken.None));
        Assert.Empty(await queries.ListForUserAsync(new UserContext(), CancellationToken.None));
    }

    [Fact]
    public void Classifier_does_not_invent_quotes()
    {
        var match = new Match
        {
            Id = "fd-cls",
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            Status = "FT",
            HomeScore = 2,
            AwayScore = 0,
            KickoffTime = DateTimeOffset.UtcNow.AddHours(-2)
        };
        var prediction = new Prediction
        {
            PredictionType = PredictionType.CorrectScore,
            PredictionValue = "2-0",
            PointsAwarded = 7
        };
        var takes = new[]
        {
            new ReceiptStoryClassifier.PunditTake(
                Guid.NewGuid(),
                "Gary Neville",
                "Chelsea to win",
                "https://sky.example/take",
                "article",
                false)
        };

        var result = ReceiptStoryClassifier.Classify(prediction, match, takes, 4, 3);

        Assert.Equal(ReceiptStoryTypes.ExactScore, result.Primary);
        Assert.Contains(ReceiptStoryTypes.BeatPundit, result.All);
        Assert.Contains(ReceiptStoryTypes.MinorityRight, result.All);
        Assert.All(result.Candidates, c =>
        {
            Assert.DoesNotContain("I think", c.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.False(c.Summary.StartsWith("\"", StringComparison.Ordinal));
        });
    }

    private static async Task<(Prediction Prediction, Match Match)> SeedFinishedPredictionAsync(
        AppDbContext db,
        string value,
        int points,
        Guid? userId = null)
    {
        var owner = userId ?? Guid.NewGuid();
        var match = new Match
        {
            Id = $"fd-rcpt-{Guid.NewGuid():N}"[..24],
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
        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = owner,
            MatchId = match.Id,
            PredictionType = PredictionType.Result,
            PredictionValue = value,
            PointsAwarded = points,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Predictions.Add(prediction);
        await db.SaveChangesAsync();
        return (prediction, match);
    }

    private static async Task SeedPunditAsync(AppDbContext db, string matchId, string prediction)
    {
        var pundit = new Pundit
        {
            Id = Guid.NewGuid(),
            Kind = PunditKind.Source,
            Name = "Gary Neville",
            NormalizedName = "gary neville",
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
    }
}
