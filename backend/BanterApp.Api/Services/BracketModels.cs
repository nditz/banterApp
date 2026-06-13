namespace BanterApp.Api.Services;

public enum BracketSlotKind
{
    GroupMatch,
    Knockout
}

public sealed record GroupQualifierRef(string Group, int Rank);

public sealed record BracketSlotDefinition(
    string SlotId,
    string MatchId,
    string Round,
    int RoundOrder,
    int Position,
    BracketSlotKind Kind,
    string? SourceSlotAId,
    string? SourceSlotBId,
    GroupQualifierRef? QualifierA,
    GroupQualifierRef? QualifierB);

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
