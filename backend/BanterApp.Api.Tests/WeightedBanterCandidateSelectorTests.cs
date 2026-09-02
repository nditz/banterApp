using BanterApp.Api.Integrations.Banter;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public class WeightedBanterCandidateSelectorTests
{
    [Fact]
    public void Select_EmptyInput_ReturnsNull()
    {
        var selector = CreateSelector(seed: 1);
        Assert.Null(selector.Select([]));
    }

    [Fact]
    public void Select_SingleCandidate_ReturnsThatCandidate()
    {
        var selector = CreateSelector(seed: 1);
        var only = Scored("only", 0.9);
        Assert.Same(only, selector.Select([only]));
    }

    [Fact]
    public void Select_TopNExcludesLowestCandidates()
    {
        var selector = CreateSelector(seed: 42, topN: 2);
        var candidates = new[]
        {
            Scored("a", 1.0),
            Scored("b", 0.9),
            Scored("c", 0.01)
        };

        var seen = new HashSet<string>();
        for (var i = 0; i < 40; i++)
        {
            var pick = CreateSelector(seed: i, topN: 2).Select(candidates);
            Assert.NotNull(pick);
            seen.Add(pick!.Candidate.ProviderContentId);
        }

        Assert.DoesNotContain("c", seen);
        Assert.Contains("a", seen);
    }

    [Fact]
    public void Select_HigherWeightsSelectedMoreOftenAcrossSeededRuns()
    {
        var candidates = new[]
        {
            Scored("high", 1.0),
            Scored("low", 0.05)
        };

        var highWins = 0;
        for (var seed = 0; seed < 200; seed++)
        {
            var pick = CreateSelector(seed, topN: 2).Select(candidates);
            if (pick!.Candidate.ProviderContentId == "high")
            {
                highWins++;
            }
        }

        Assert.True(highWins > 120, $"Expected high-score candidate to win most often, won {highWins}/200");
    }

    private static WeightedBanterCandidateSelector CreateSelector(int seed, int topN = 15) =>
        new(
            Options.Create(new BanterOptions { TopCandidatePoolSize = topN }),
            new SeededBanterRandom(seed));

    private static ScoredBanterCandidate Scored(string id, double score) =>
        new(
            new BanterCandidate("giphy", id, BanterContentType.Gif, "q", $"https://x/{id}", 0, []),
            Relevance: score,
            Freshness: score,
            Popularity: score,
            Novelty: score,
            FinalScore: score);
}
