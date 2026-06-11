using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Integrations.SportsData;

public sealed class MockSportsDataProvider : ISportsDataProvider
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static readonly IReadOnlyList<MatchDto> AllFixtures =
    [
        Finished("wc26-001", "USA", "United States", "USA", "CAN", "Canada", "CAN",
            Now.AddDays(-3), "Group A", "A", "MetLife Stadium, New Jersey", 2, 1),
        Finished("wc26-002", "MEX", "Mexico", "MEX", "JAM", "Jamaica", "JAM",
            Now.AddDays(-2), "Group A", "A", "Estadio Azteca, Mexico City", 3, 0),
        Finished("wc26-003", "BRA", "Brazil", "BRA", "SRB", "Serbia", "SRB",
            Now.AddDays(-1), "Group C", "C", "SoFi Stadium, Los Angeles", 1, 1),
        Finished("wc26-004", "ARG", "Argentina", "ARG", "POL", "Poland", "POL",
            Now.AddHours(-6), "Group D", "D", "Hard Rock Stadium, Miami", 2, 0),
        Upcoming("wc26-005", "ENG", "England", "ENG", "FRA", "France", "FRA",
            Now.AddDays(1).AddHours(19), "Group B", "B", "AT&T Stadium, Dallas"),
        Upcoming("wc26-006", "ESP", "Spain", "ESP", "GER", "Germany", "GER",
            Now.AddDays(2).AddHours(16), "Group B", "B", "Mercedes-Benz Stadium, Atlanta"),
        Upcoming("wc26-007", "POR", "Portugal", "POR", "MAR", "Morocco", "MAR",
            Now.AddDays(3).AddHours(20), "Group F", "F", "Lumen Field, Seattle"),
        Upcoming("wc26-008", "NED", "Netherlands", "NED", "JPN", "Japan", "JPN",
            Now.AddDays(4).AddHours(14), "Group E", "E", "BC Place, Vancouver"),
        Upcoming("wc26-009", "COL", "Colombia", "COL", "URU", "Uruguay", "URU",
            Now.AddDays(5).AddHours(18), "Group G", "G", "Levi's Stadium, San Francisco"),
        Upcoming("wc26-010", "SEN", "Senegal", "SEN", "KOR", "South Korea", "KOR",
            Now.AddDays(6).AddHours(15), "Group H", "H", "BMO Field, Toronto"),
        Upcoming("wc26-011", "ITA", "Italy", "ITA", "CRO", "Croatia", "CRO",
            Now.AddDays(7).AddHours(21), "Round of 16", "", "NRG Stadium, Houston"),
        Upcoming("wc26-012", "BEL", "Belgium", "BEL", "SUI", "Switzerland", "SUI",
            Now.AddDays(8).AddHours(17), "Round of 16", "", "Lincoln Financial Field, Philadelphia"),
    ];

    private static readonly Dictionary<string, MatchStatisticsDto> Statistics = new()
    {
        ["wc26-001"] = new("wc26-001", 54, 46, 14, 9, 6, 3, 7, 4, 11, 13, 2, 3, 0, 0),
        ["wc26-002"] = new("wc26-002", 61, 39, 18, 7, 9, 2, 5, 2, 8, 14, 1, 2, 0, 0),
        ["wc26-003"] = new("wc26-003", 58, 42, 16, 11, 5, 4, 6, 3, 10, 12, 3, 2, 0, 0),
        ["wc26-004"] = new("wc26-004", 52, 48, 12, 8, 7, 2, 4, 5, 9, 11, 2, 1, 0, 0),
    };

    private static readonly Dictionary<string, IReadOnlyList<StandingDto>> StandingsByGroup = new()
    {
        ["A"] =
        [
            Standing(1, "MEX", "Mexico", "MEX", 2, 2, 0, 0, 5, 1, 4, 6),
            Standing(2, "USA", "United States", "USA", 2, 1, 1, 0, 3, 2, 1, 4),
            Standing(3, "JAM", "Jamaica", "JAM", 2, 0, 0, 2, 1, 5, -4, 0),
        ],
        ["B"] =
        [
            Standing(1, "FRA", "France", "FRA", 1, 1, 0, 0, 2, 0, 2, 3),
            Standing(2, "ENG", "England", "ENG", 1, 1, 0, 0, 2, 1, 1, 3),
            Standing(3, "GER", "Germany", "GER", 1, 0, 0, 1, 0, 2, -2, 0),
        ],
        ["C"] =
        [
            Standing(1, "BRA", "Brazil", "BRA", 1, 0, 1, 0, 1, 1, 0, 1),
            Standing(2, "SRB", "Serbia", "SRB", 1, 0, 1, 0, 1, 1, 0, 1),
        ],
        ["D"] =
        [
            Standing(1, "ARG", "Argentina", "ARG", 1, 1, 0, 0, 2, 0, 2, 3),
            Standing(2, "POL", "Poland", "POL", 1, 0, 0, 1, 0, 2, -2, 0),
        ],
    };

    public Task<IReadOnlyList<MatchDto>> GetUpcomingFixturesAsync(CancellationToken cancellationToken = default)
    {
        var upcoming = AllFixtures
            .Where(m => m.Status is "NS" or "TBD" or "Scheduled")
            .OrderBy(m => m.KickoffUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<MatchDto>>(upcoming);
    }

    public Task<IReadOnlyList<MatchDto>> GetResultsAsync(CancellationToken cancellationToken = default)
    {
        var results = AllFixtures
            .Where(m => m.Status == "FT")
            .OrderByDescending(m => m.KickoffUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<MatchDto>>(results);
    }

    public Task<MatchStatisticsDto?> GetMatchStatisticsAsync(
        string matchId,
        CancellationToken cancellationToken = default)
    {
        Statistics.TryGetValue(matchId, out var stats);
        return Task.FromResult(stats);
    }

    public Task<IReadOnlyList<StandingDto>> GetStandingsAsync(
        string group,
        CancellationToken cancellationToken = default)
    {
        var key = group.Trim().ToUpperInvariant();
        if (StandingsByGroup.TryGetValue(key, out var standings))
        {
            return Task.FromResult(standings);
        }

        return Task.FromResult<IReadOnlyList<StandingDto>>([]);
    }

    private static MatchDto Finished(
        string id,
        string homeId, string homeName, string homeCode,
        string awayId, string awayName, string awayCode,
        DateTimeOffset kickoff, string stage, string group, string venue,
        int homeScore, int awayScore) =>
        new(
            id,
            Team(homeId, homeName, homeCode),
            Team(awayId, awayName, awayCode),
            kickoff,
            stage,
            group,
            venue,
            "FT",
            homeScore,
            awayScore);

    private static MatchDto Upcoming(
        string id,
        string homeId, string homeName, string homeCode,
        string awayId, string awayName, string awayCode,
        DateTimeOffset kickoff, string stage, string group, string venue) =>
        new(
            id,
            Team(homeId, homeName, homeCode),
            Team(awayId, awayName, awayCode),
            kickoff,
            stage,
            group,
            venue,
            "NS",
            null,
            null);

    private static TeamDto Team(string id, string name, string code) =>
        new(id, name, code, code);

    private static StandingDto Standing(
        int rank,
        string teamId, string teamName, string teamCode,
        int played, int won, int drawn, int lost,
        int goalsFor, int goalsAgainst, int goalDiff, int points) =>
        new(
            rank,
            Team(teamId, teamName, teamCode),
            played,
            won,
            drawn,
            lost,
            goalsFor,
            goalsAgainst,
            goalDiff,
            points);
}
