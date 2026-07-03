using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Pundits;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Feed;

public sealed class FeedRelevanceScorer
{
    private readonly ProcessingOptions _processing;
    private readonly SourceWeightsOptions _sourceWeights;
    private readonly MediaIngestOptions _mediaIngest;

    public FeedRelevanceScorer(
        IOptions<ProcessingOptions> processing,
        IOptions<SourceWeightsOptions> sourceWeights,
        IOptions<MediaIngestOptions> mediaIngest)
    {
        _processing = processing.Value;
        _sourceWeights = sourceWeights.Value;
        _mediaIngest = mediaIngest.Value;
    }

    public FeedRelevanceResult Score(
        NewsFeedItem item,
        PunditOpinion? linkedOpinion = null,
        MediaSource? mediaSource = null,
        bool isDuplicate = false)
    {
        var score = 0;
        var reasons = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.MatchId))
        {
            var kickoff = linkedOpinion?.Match?.KickoffTime;
            if (kickoff is null || Math.Abs((kickoff.Value - DateTimeOffset.UtcNow).TotalHours) <= 48)
            {
                score += 50;
                reasons.Add("current_match");
            }
        }

        var hasPrediction = !string.IsNullOrWhiteSpace(linkedOpinion?.Prediction) ||
                            !string.IsNullOrWhiteSpace(item.PredictionSummary);

        if (hasPrediction)
        {
            score += 30;
            reasons.Add("has_prediction");
        }

        score += ScoreCredibility(item, mediaSource, reasons);
        score += ScoreFreshness(item.PublishedAt, reasons);
        score += ScoreEngagement(item, linkedOpinion, reasons);
        score += ScoreUniqueness(isDuplicate, reasons);

        var band = score >= _processing.PredictionExtraction.MinFeedQualityScore
            ? FeedQualityBand.Pass
            : score >= 40
                ? FeedQualityBand.Review
                : FeedQualityBand.Reject;

        return new FeedRelevanceResult(score, band, reasons);
    }

    public bool ShouldGenerateBanter(FeedRelevanceResult result) =>
        result.Score >= _processing.PredictionExtraction.MinBanterQualityScore;

    private int ScoreCredibility(NewsFeedItem item, MediaSource? mediaSource, List<string> reasons)
    {
        var weight = 1.0;
        if (mediaSource is not null)
        {
            weight = ConfidenceScoringHelper.ResolveSourceWeight(mediaSource, _mediaIngest);
        }
        else if (_sourceWeights.DefaultWeights.TryGetValue(item.Category ?? "website", out var defaultWeight))
        {
            weight = defaultWeight;
        }

        if (weight >= 0.9)
        {
            reasons.Add("high_credibility");
            return 20;
        }

        if (weight < 0.7)
        {
            reasons.Add("low_credibility");
            return -10;
        }

        reasons.Add("medium_credibility");
        return 10;
    }

    private static int ScoreFreshness(DateTimeOffset publishedAt, List<string> reasons)
    {
        var age = DateTimeOffset.UtcNow - publishedAt;
        if (age <= TimeSpan.FromHours(1))
        {
            reasons.Add("fresh_hour");
            return 40;
        }

        if (age <= TimeSpan.FromDays(1))
        {
            reasons.Add("fresh_day");
            return 20;
        }

        reasons.Add("stale");
        return -20;
    }

    private static int ScoreEngagement(
        NewsFeedItem item,
        PunditOpinion? linkedOpinion,
        List<string> reasons)
    {
        var high = !string.IsNullOrWhiteSpace(item.ImageUrl) ||
                   linkedOpinion?.IsDirectQuote == true ||
                   !string.IsNullOrWhiteSpace(item.MatchId);

        if (high)
        {
            reasons.Add("engagement_high");
            return 25;
        }

        reasons.Add("engagement_low");
        return -15;
    }

    private static int ScoreUniqueness(bool isDuplicate, List<string> reasons)
    {
        if (isDuplicate)
        {
            reasons.Add("duplicate");
            return -25;
        }

        reasons.Add("unique");
        return 15;
    }
}

public enum FeedQualityBand
{
    Reject,
    Review,
    Pass
}

public sealed record FeedRelevanceResult(
    int Score,
    FeedQualityBand Band,
    IReadOnlyList<string> Reasons);
