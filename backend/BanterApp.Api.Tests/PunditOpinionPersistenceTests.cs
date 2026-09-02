using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Pundits;
using BanterApp.Api.Integrations.Pundits.Dtos;
using BanterApp.Api.Integrations.Rss;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class PunditOpinionPersistenceTests
{
    [Fact]
    public async Task PersistExtractionAsync_writes_pundit_prediction_when_match_resolved()
    {
        await using var db = CreateDb();
        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "Sky Sports",
            SourceType = "website",
            ExtractPredictions = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var item = new MediaItem
        {
            Id = Guid.NewGuid(),
            MediaSourceId = source.Id,
            MediaSource = source,
            ExternalId = "article-1",
            Title = "Preview",
            SourceUrl = "https://example.com/preview",
            RawText = new string('x', 400),
            ProcessingStatus = MediaItemProcessingStatus.Enriched,
            PublishedAt = DateTimeOffset.UtcNow
        };
        db.MediaSources.Add(source);
        db.MediaItems.Add(item);
        db.Matches.Add(new Match
        {
            Id = "wc-usa-mex",
            TeamA = "United States",
            TeamB = "Mexico",
            TeamACode = "USA",
            TeamBCode = "MEX",
            KickoffTime = DateTimeOffset.UtcNow.AddDays(2),
            Stage = "Group",
            Group = "A",
            Venue = "Test"
        });
        await db.SaveChangesAsync();

        var extraction = new PunditExtractionResult(
            "article",
            "Sky Sports",
            item.SourceUrl,
            item.Title,
            item.PublishedAt,
            [
                new PunditExtractionPunditDto(
                    "Gary Neville",
                    "pundit",
                    [
                        new PunditExtractionOpinionDto(
                            "Premier League",
                            "United States",
                            null,
                            "United States vs Mexico",
                            "wc-usa-mex",
                            "USA look strong in transition.",
                            "United States to win",
                            "match_result",
                            0.82,
                            "USA will win this one.",
                            "Preview segment",
                            true,
                            false)
                    ])
            ],
            [],
            "Preview summary",
            "{}");

        var service = CreatePersistenceService(db);
        var created = await service.PersistExtractionAsync(item, extraction, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(1, created);
        var opinion = await db.PunditOpinions.SingleAsync();
        Assert.Equal("wc-usa-mex", opinion.MatchId);

        var prediction = await db.PunditPredictions.SingleAsync();
        Assert.Equal("wc-usa-mex", prediction.MatchId);
        Assert.Equal("United States to win", prediction.Prediction);

        var feedItem = await db.NewsFeedItems.SingleAsync();
        Assert.Equal("wc-usa-mex", feedItem.MatchId);
        Assert.NotNull(feedItem.QualityScore);
    }

    [Fact]
    public async Task PersistExtractionAsync_uses_article_author_when_pundit_unknown()
    {
        await using var db = CreateDb();
        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "The Guardian",
            SourceType = "website",
            ExtractPredictions = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var item = new MediaItem
        {
            Id = Guid.NewGuid(),
            MediaSourceId = source.Id,
            MediaSource = source,
            ExternalId = "article-2",
            Title = "Column",
            Author = "Jonathan Wilson",
            SourceUrl = "https://example.com/column",
            RawText = new string('y', 400),
            ProcessingStatus = MediaItemProcessingStatus.Enriched,
            PublishedAt = DateTimeOffset.UtcNow
        };
        db.MediaSources.Add(source);
        db.MediaItems.Add(item);
        await db.SaveChangesAsync();

        var extraction = new PunditExtractionResult(
            "article",
            "The Guardian",
            item.SourceUrl,
            item.Title,
            item.PublishedAt,
            [
                new PunditExtractionPunditDto(
                    "Unknown",
                    "journalist",
                    [
                        new PunditExtractionOpinionDto(
                            "Premier League",
                            "France",
                            null,
                            null,
                            null,
                            "France remain favourites.",
                            "France to reach the final",
                            "general_opinion",
                            0.72,
                            null,
                            null,
                            false,
                            false)
                    ])
            ],
            [],
            "Column summary",
            "{}");

        var service = CreatePersistenceService(db);
        await service.PersistExtractionAsync(item, extraction, CancellationToken.None);
        await db.SaveChangesAsync();

        var pundit = await db.Pundits.SingleAsync();
        Assert.Equal("Jonathan Wilson", pundit.Name);
    }

    private static PunditOpinionPersistenceService CreatePersistenceService(AppDbContext db)
    {
        var review = new PunditReviewFlagger(Options.Create(new PunditIngestOptions
        {
            MinConfidenceWithoutReview = 0.6,
            AutoApproveConfidence = 0.55,
            MinSourceTextLength = 200
        }));
        var processing = Options.Create(new ProcessingOptions());
        var rssSeed = new StaticRssFeedCatalogSeed();
        var resolver = new ReactionMediaResolver(
            new NullReactionGifProvider(),
            new InMemoryReactionGifLedger(),
            NullLogger<ReactionMediaResolver>.Instance);
        return new PunditOpinionPersistenceService(
            db,
            review,
            resolver,
            new MatchResolutionService(db),
            new FeedRelevanceScorer(processing, Options.Create(new SourceWeightsOptions()), rssSeed),
            processing,
            rssSeed);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
