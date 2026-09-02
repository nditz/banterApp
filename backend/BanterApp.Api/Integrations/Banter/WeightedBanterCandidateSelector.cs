using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Banter;

/// <summary>
/// Selects from the top-N scored candidates using weighted randomness (never always #1).
/// </summary>
public sealed class WeightedBanterCandidateSelector : IBanterCandidateSelector
{
    private readonly BanterOptions _options;
    private readonly IBanterRandom _random;

    public WeightedBanterCandidateSelector(IOptions<BanterOptions> options, IBanterRandom random)
    {
        _options = options.Value;
        _random = random;
    }

    public ScoredBanterCandidate? Select(IReadOnlyList<ScoredBanterCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var topN = candidates
            .OrderByDescending(c => c.FinalScore)
            .Take(_options.TopCandidatePoolSize)
            .ToList();

        if (topN.Count == 1)
        {
            return topN[0];
        }

        var weights = topN.Select(c => Math.Max(0.01, c.FinalScore)).ToArray();
        var total = weights.Sum();
        var roll = _random.NextDouble() * total;
        var cumulative = 0.0;

        for (var i = 0; i < topN.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
            {
                return topN[i];
            }
        }

        return topN[^1];
    }
}
