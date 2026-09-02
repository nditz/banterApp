using BanterApp.Api.Integrations.Banter;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public class BanterCandidateScorerTests
{
    [Fact]
    public void Score_NoveltyAffectsFinalScore()
    {
        var scorer = CreateScorer();
        var candidate = Cand("a", rank: 0, query: "fresh query");
        var exclusions = new BanterExclusionContext();
        exclusions.SearchPhrases.Add("stale query");

        var highNovelty = scorer.Score(EmptyContext(), [candidate], BanterExclusionContext.Empty).Single();
        exclusions.SearchPhrases.Add("fresh query");
        var lowerNovelty = scorer.Score(EmptyContext(), [candidate], exclusions).Single();

        Assert.True(highNovelty.Novelty > lowerNovelty.Novelty);
        Assert.True(highNovelty.FinalScore > lowerNovelty.FinalScore);
    }

    [Fact]
    public void Score_RelevancePrefersEarlierProviderRank()
    {
        var scorer = CreateScorer();
        var scored = scorer.Score(
            EmptyContext(),
            [Cand("a", 0), Cand("b", 10)],
            BanterExclusionContext.Empty);

        Assert.Equal("a", scored[0].Candidate.ProviderContentId);
        Assert.True(scored[0].Relevance > scored[1].Relevance);
    }

    [Fact]
    public void Score_UsesConfiguredWeights()
    {
        var scorer = CreateScorer(relevance: 1, freshness: 0, popularity: 0, novelty: 0);
        var scored = scorer.Score(EmptyContext(), [Cand("a", 0)], BanterExclusionContext.Empty).Single();
        Assert.Equal(scored.Relevance, scored.FinalScore, 5);
    }

    [Fact]
    public void Score_ExcludesProviderIdsFromPool()
    {
        var exclusions = new BanterExclusionContext();
        exclusions.ProviderContentIds.Add("blocked");
        var scored = CreateScorer().Score(
            EmptyContext(),
            [Cand("blocked", 0), Cand("ok", 1)],
            exclusions);

        Assert.Single(scored);
        Assert.Equal("ok", scored[0].Candidate.ProviderContentId);
    }

    [Fact]
    public void BanterOptions_NormalizesInvalidWeights()
    {
        var opts = new BanterOptions
        {
            Weights = new BanterScoreWeights
            {
                Relevance = -1,
                Freshness = 0,
                Popularity = 0,
                Novelty = 0
            }
        };
        opts.ValidateOrNormalize();
        Assert.True(opts.Weights.Relevance > 0);
        Assert.Equal(1.0, opts.Weights.Relevance + opts.Weights.Freshness + opts.Weights.Popularity + opts.Weights.Novelty, 5);
    }

    private static BanterCandidateScorer CreateScorer(
        double relevance = 0.4,
        double freshness = 0.25,
        double popularity = 0.15,
        double novelty = 0.2)
    {
        var opts = new BanterOptions
        {
            Weights = new BanterScoreWeights
            {
                Relevance = relevance,
                Freshness = freshness,
                Popularity = popularity,
                Novelty = novelty
            }
        };
        opts.ValidateOrNormalize();
        return new BanterCandidateScorer(Options.Create(opts));
    }

    private static BanterCandidate Cand(string id, double rank, string query = "q") =>
        new("giphy", id, BanterContentType.Gif, query, $"https://giphy.com/{id}.gif", rank, [query]);

    private static BanterContext EmptyContext() =>
        new(null, null, null, null, null, null,
            PredictionOutcomeKind.Unknown, MatchOutcomeKind.Unknown,
            null, null, null);
}
