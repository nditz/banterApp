namespace BanterApp.Api.Integrations.Banter;

/// <summary>
/// Feature-flagged Banter Strategy Engine settings. Defaults keep the legacy media path.
/// </summary>
public sealed class BanterOptions
{
    public const string SectionName = "Banter";

    /// <summary>When false (default), generation uses <see cref="LegacyBanterGenerator"/> only.</summary>
    public bool UseStrategyEngine { get; set; }

    public int RecentContentWindowDays { get; set; } = 30;

    public int RecentTeamContentWindowDays { get; set; } = 14;

    /// <summary>Short global hard-repeat window for highly visible provider IDs.</summary>
    public int GlobalHardRepeatWindowDays { get; set; } = 3;

    public int ConceptCount { get; set; } = 12;

    public int ConceptsUsedPerGeneration { get; set; } = 4;

    public int CandidatesPerConcept { get; set; } = 15;

    public int TopCandidatePoolSize { get; set; } = 15;

    public BanterScoreWeights Weights { get; set; } = new();

    public void ValidateOrNormalize()
    {
        ConceptCount = Math.Max(1, ConceptCount);
        ConceptsUsedPerGeneration = Math.Clamp(ConceptsUsedPerGeneration, 1, ConceptCount);
        CandidatesPerConcept = Math.Clamp(CandidatesPerConcept, 1, 50);
        TopCandidatePoolSize = Math.Max(1, TopCandidatePoolSize);
        RecentContentWindowDays = Math.Max(1, RecentContentWindowDays);
        RecentTeamContentWindowDays = Math.Max(1, RecentTeamContentWindowDays);
        GlobalHardRepeatWindowDays = Math.Max(1, GlobalHardRepeatWindowDays);
        Weights.Normalize();
    }
}

public sealed class BanterScoreWeights
{
    public double Relevance { get; set; } = 0.40;
    public double Freshness { get; set; } = 0.25;
    public double Popularity { get; set; } = 0.15;
    public double Novelty { get; set; } = 0.20;

    public void Normalize()
    {
        Relevance = Math.Max(0, Relevance);
        Freshness = Math.Max(0, Freshness);
        Popularity = Math.Max(0, Popularity);
        Novelty = Math.Max(0, Novelty);

        var sum = Relevance + Freshness + Popularity + Novelty;
        if (sum <= 0)
        {
            Relevance = 0.40;
            Freshness = 0.25;
            Popularity = 0.15;
            Novelty = 0.20;
            return;
        }

        Relevance /= sum;
        Freshness /= sum;
        Popularity /= sum;
        Novelty /= sum;
    }
}
