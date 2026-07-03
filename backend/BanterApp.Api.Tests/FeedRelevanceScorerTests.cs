using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class FeedRelevanceScorerTests
{
    private readonly FeedRelevanceScorer _scorer = CreateScorer();

    [Fact]
    public void Score_passes_when_prediction_and_match_present()
    {
        var item = new NewsFeedItem
        {
            Id = "test-1",
            Source = "Sky Sports",
            Title = "Neville backs USA",
            Summary = "Prediction take",
            Url = "https://example.com/1",
            Category = "pundit_quote",
            MatchId = "wc-usa-mex",
            PredictionSummary = "USA to win",
            PublishedAt = DateTimeOffset.UtcNow.AddMinutes(30),
            ImageUrl = "https://media.example/gif.gif",
            MediaType = "gif"
        };

        var opinion = new PunditOpinion
        {
            Prediction = "USA to win",
            Confidence = 0.8,
            IsDirectQuote = true,
            Match = new Match
            {
                Id = "wc-usa-mex",
                TeamA = "USA",
                TeamB = "Mexico",
                TeamACode = "USA",
                TeamBCode = "MEX",
                KickoffTime = DateTimeOffset.UtcNow.AddHours(6),
                Stage = "Group",
                Group = "A",
                Venue = "Test"
            }
        };

        var result = _scorer.Score(item, opinion);

        Assert.Equal(FeedQualityBand.Pass, result.Band);
        Assert.True(result.Score >= 60);
        Assert.Contains("has_prediction", result.Reasons);
    }

    [Fact]
    public void Score_rejects_stale_duplicate_items()
    {
        var item = new NewsFeedItem
        {
            Id = "test-2",
            Source = "Blog",
            Title = "Old take",
            Summary = "Nothing new",
            Url = "https://example.com/2",
            Category = "sports_news",
            PublishedAt = DateTimeOffset.UtcNow.AddDays(3)
        };

        var result = _scorer.Score(item, isDuplicate: true);

        Assert.Equal(FeedQualityBand.Reject, result.Band);
    }

    [Fact]
    public void ShouldGenerateBanter_requires_threshold()
    {
        var low = new FeedRelevanceResult(55, FeedQualityBand.Review, []);
        var high = new FeedRelevanceResult(75, FeedQualityBand.Pass, []);

        Assert.False(_scorer.ShouldGenerateBanter(low));
        Assert.True(_scorer.ShouldGenerateBanter(high));
    }

    private static FeedRelevanceScorer CreateScorer()
    {
        var processing = Options.Create(new ProcessingOptions
        {
            PredictionExtraction = new PredictionExtractionProcessingOptions
            {
                MinFeedQualityScore = 60,
                MinBanterQualityScore = 70
            }
        });
        var weights = Options.Create(new SourceWeightsOptions());
        var media = Options.Create(new MediaIngestOptions());
        return new FeedRelevanceScorer(processing, weights, media);
    }
}
