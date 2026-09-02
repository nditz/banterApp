using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Banter;

public sealed class BanterCandidateScorer : IBanterCandidateScorer
{
    private readonly BanterOptions _options;

    public BanterCandidateScorer(IOptions<BanterOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<ScoredBanterCandidate> Score(
        BanterContext context,
        IEnumerable<BanterCandidate> candidates,
        BanterExclusionContext exclusions)
    {
        var weights = _options.Weights;
        var scored = new List<ScoredBanterCandidate>();

        foreach (var candidate in candidates)
        {
            if (exclusions.IsProviderIdExcluded(candidate.ProviderContentId))
            {
                continue;
            }

            var relevance = ScoreRelevance(candidate);
            var freshness = 0.5; // Giphy search does not expose reliable timestamps in our parser
            var popularity = ScorePopularity(candidate);
            var novelty = ScoreNovelty(candidate, exclusions);
            var final =
                weights.Relevance * relevance +
                weights.Freshness * freshness +
                weights.Popularity * popularity +
                weights.Novelty * novelty;

            scored.Add(new ScoredBanterCandidate(
                candidate,
                relevance,
                freshness,
                popularity,
                novelty,
                final));
        }

        return scored
            .OrderByDescending(s => s.FinalScore)
            .ThenBy(s => s.Candidate.ProviderRank)
            .ToList();
    }

    private static double ScoreRelevance(BanterCandidate candidate)
    {
        // Prefer earlier provider ranks and query-tag overlap.
        var rankScore = 1.0 / (1.0 + Math.Max(0, candidate.ProviderRank));
        var tagBonus = candidate.Tags.Count > 0 ? 0.15 : 0;
        return Math.Clamp(rankScore + tagBonus, 0, 1);
    }

    private static double ScorePopularity(BanterCandidate candidate) =>
        Math.Clamp(1.0 - (candidate.ProviderRank / 25.0), 0.05, 1.0);

    private static double ScoreNovelty(BanterCandidate candidate, BanterExclusionContext exclusions)
    {
        if (exclusions.IsProviderIdExcluded(candidate.ProviderContentId))
        {
            return 0;
        }

        if (exclusions.IsSearchPhraseExcluded(candidate.SourceQuery))
        {
            return 0.35;
        }

        return 1.0;
    }
}
