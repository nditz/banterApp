namespace BanterApp.Api.Services;

public enum BracketSlotKind
{
    GroupMatch,
    Knockout
}

public abstract record TeamSource;

public sealed record GroupRankSource(string Group, int Rank) : TeamSource;

public sealed record ThirdPlaceSource(params string[] CandidateGroups) : TeamSource;

/// <summary>Third-placed team assigned by FIFA Annex C to face the group winner (e.g. 1A).</summary>
public sealed record AnnexCThirdSource(string GroupWinnerLetter) : TeamSource;

public sealed record SlotWinnerSource(string SlotId) : TeamSource;

public sealed record SlotLoserSource(string SlotId) : TeamSource;

public sealed record BracketSlotDefinition(
    string SlotId,
    string MatchId,
    string Round,
    int RoundOrder,
    int Position,
    BracketSlotKind Kind,
    TeamSource? TeamSourceA,
    TeamSource? TeamSourceB);

public sealed record GroupStandingEntry(
    string TeamCode,
    string TeamName,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    int Rank);

public sealed record BracketTeamInfo(string Code, string Name);
