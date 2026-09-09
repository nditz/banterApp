using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Admin;
using BanterApp.Api.Integrations.Pundits;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

public class AdminReviewServiceTests
{
    [Fact]
    public async Task ListPendingAsync_ReturnsOnlyPendingReviewItems()
    {
        await using var db = TestDbContextFactory.Create();
        await SeedOpinionAsync(db, needsReview: true, reviewStatus: "pending");
        await SeedOpinionAsync(db, needsReview: false, reviewStatus: "approved");

        var service = CreateService(db);
        var pending = await service.ListPendingAsync(CancellationToken.None);

        Assert.Single(pending);
    }

    [Fact]
    public async Task ApproveAsync_ClearsReviewFlagAndSetsApproved()
    {
        await using var db = TestDbContextFactory.Create();
        var opinionId = await SeedOpinionAsync(db, needsReview: true, reviewStatus: "pending");
        var service = CreateService(db);
        var admin = new UserContext { UserId = Guid.NewGuid() };

        await service.ApproveAsync(opinionId, admin, CancellationToken.None);

        var saved = await db.PunditOpinions.FindAsync(opinionId);
        Assert.NotNull(saved);
        Assert.False(saved!.NeedsHumanReview);
        Assert.Equal("approved", saved.ReviewStatus);
        Assert.NotNull(saved.ReviewedAt);
    }

    [Fact]
    public async Task RejectAsync_SetsRejectedStatus()
    {
        await using var db = TestDbContextFactory.Create();
        var opinionId = await SeedOpinionAsync(db, needsReview: true, reviewStatus: "pending");
        var service = CreateService(db);
        var admin = new UserContext { UserId = Guid.NewGuid() };

        await service.RejectAsync(opinionId, admin, "Low confidence", CancellationToken.None);

        var saved = await db.PunditOpinions.FindAsync(opinionId);
        Assert.NotNull(saved);
        Assert.Equal("rejected", saved!.ReviewStatus);
        Assert.Equal("Low confidence", saved.ReviewNotes);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOpinionFields()
    {
        await using var db = TestDbContextFactory.Create();
        var opinionId = await SeedOpinionAsync(db, needsReview: true, reviewStatus: "pending");
        var service = CreateService(db);
        var admin = new UserContext { UserId = Guid.NewGuid() };

        await service.UpdateAsync(
            opinionId,
            new AdminReviewUpdateRequest(
                Opinion: "Corrected opinion text",
                Prediction: "England win group",
                PredictionType: "group_winner",
                PunditName: "Gary Neville",
                IsDirectQuote: true,
                QuoteContext: "TV segment",
                ReviewNotes: null),
            admin,
            CancellationToken.None);

        var saved = await db.PunditOpinions
            .Include(o => o.Pundit)
            .FirstAsync(o => o.Id == opinionId);

        Assert.Equal("Corrected opinion text", saved.Opinion);
        Assert.Equal("England win group", saved.Prediction);
        Assert.Equal("Gary Neville", saved.Pundit.Name);
        Assert.True(saved.IsDirectQuote);
    }

    [Fact]
    public async Task ApproveAsync_writes_match_linked_pundit_prediction()
    {
        await using var db = TestDbContextFactory.Create();
        db.Matches.Add(new Match
        {
            Id = "fd-test-1",
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            TeamACode = "ARS",
            TeamBCode = "CHE",
            KickoffTime = DateTimeOffset.UtcNow.AddDays(1),
            Stage = "League",
            Venue = "Emirates"
        });
        var opinionId = await SeedOpinionAsync(
            db,
            needsReview: true,
            reviewStatus: "pending",
            matchId: "fd-test-1",
            prediction: "Arsenal to win",
            predictionType: "match_result",
            kind: PunditKind.Source);
        var service = CreateService(db);

        await service.ApproveAsync(opinionId, new UserContext { UserId = Guid.NewGuid() }, CancellationToken.None);

        var linked = await db.PunditPredictions.SingleAsync();
        Assert.Equal("fd-test-1", linked.MatchId);
        Assert.Equal("Arsenal to win", linked.Prediction);
        Assert.True(linked.IsMatched);
        Assert.Equal("https://example.com", linked.SourceUrl);
    }

    private static AdminReviewService CreateService(AppDbContext db) =>
        new(db, new FakeRecurringJobManager(), new PunditMatchPredictionSync(db));

    private static async Task<Guid> SeedOpinionAsync(
        BanterApp.Api.Data.AppDbContext db,
        bool needsReview,
        string reviewStatus,
        string? matchId = null,
        string? prediction = null,
        string? predictionType = null,
        PunditKind kind = PunditKind.Persona)
    {
        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "BBC",
            SourceType = "rss",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var item = new MediaItem
        {
            Id = Guid.NewGuid(),
            MediaSourceId = source.Id,
            ExternalId = Guid.NewGuid().ToString("N"),
            Title = "Test article",
            SourceUrl = "https://example.com",
            ProcessingStatus = MediaItemProcessingStatus.Extracted,
            LastSyncedAt = DateTimeOffset.UtcNow
        };
        var pundit = new Pundit
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            Name = "Unknown",
            NormalizedName = "unknown",
            AttributionMode = PunditAttributionMode.Licensed
        };
        var opinion = new PunditOpinion
        {
            Id = Guid.NewGuid(),
            SourceItemId = item.Id,
            PunditId = pundit.Id,
            Opinion = "Original opinion",
            Prediction = prediction,
            PredictionType = predictionType,
            MatchId = matchId,
            NeedsHumanReview = needsReview,
            ReviewStatus = reviewStatus,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.MediaSources.Add(source);
        db.MediaItems.Add(item);
        db.Pundits.Add(pundit);
        db.PunditOpinions.Add(opinion);
        await db.SaveChangesAsync();
        return opinion.Id;
    }
}
