using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Integrations.SportsData;

public sealed class MockSportsDataProvider : ISportsDataProvider, ISportsDataEnrichment
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static readonly IReadOnlyList<MatchDto> AllFixtures = BuildFixtures();

    private static readonly Dictionary<string, MatchStatisticsDto> Statistics = new()
    {
        ["wc26-grp-A-1"] = new("wc26-grp-A-1", 54, 46, 14, 9, 6, 3, 7, 4, 11, 13, 2, 3, 0, 0),
        ["wc26-grp-A-2"] = new("wc26-grp-A-2", 61, 39, 18, 7, 9, 2, 5, 2, 8, 14, 1, 2, 0, 0),
    };

    public Task<IReadOnlyList<MatchDto>> GetAllFixturesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(AllFixtures);

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

    public Task<IReadOnlyList<MatchDto>> GetLiveFixturesAsync(CancellationToken cancellationToken = default)
    {
        var live = AllFixtures
            .Where(m => m.Status is "LIVE" or "1H" or "2H" or "HT")
            .ToList();
        return Task.FromResult<IReadOnlyList<MatchDto>>(live);
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
        var matches = AllFixtures.Where(m => string.Equals(m.Group, key, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<StandingDto>>([]);
        }

        var table = ComputeStandingsFromResults(matches);
        return Task.FromResult(table);
    }

    private static IReadOnlyList<StandingDto> ComputeStandingsFromResults(IReadOnlyList<MatchDto> matches)
    {
        var teams = new Dictionary<string, (TeamDto Team, int Played, int Won, int Drawn, int Lost, int Gf, int Ga, int Points)>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in matches.Where(m => m.Status == "FT"))
        {
            Ensure(teams, match.HomeTeam);
            Ensure(teams, match.AwayTeam);
            var home = teams[match.HomeTeam.Code];
            var away = teams[match.AwayTeam.Code];
            var hg = match.HomeScore ?? 0;
            var ag = match.AwayScore ?? 0;
            home.Played++; away.Played++;
            home.Gf += hg; home.Ga += ag;
            away.Gf += ag; away.Ga += hg;
            if (hg > ag) { home.Won++; home.Points += 3; away.Lost++; }
            else if (hg < ag) { away.Won++; away.Points += 3; home.Lost++; }
            else { home.Drawn++; away.Drawn++; home.Points++; away.Points++; }
            teams[match.HomeTeam.Code] = home;
            teams[match.AwayTeam.Code] = away;
        }

        return teams.Values
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.Gf - t.Ga)
            .Select((entry, index) => Standing(
                index + 1,
                entry.Team.Id,
                entry.Team.Name,
                entry.Team.Code,
                entry.Played,
                entry.Won,
                entry.Drawn,
                entry.Lost,
                entry.Gf,
                entry.Ga,
                entry.Gf - entry.Ga,
                entry.Points))
            .ToList();
    }

    private static void Ensure(
        Dictionary<string, (TeamDto Team, int Played, int Won, int Drawn, int Lost, int Gf, int Ga, int Points)> teams,
        TeamDto team)
    {
        if (!teams.ContainsKey(team.Code))
        {
            teams[team.Code] = (team, 0, 0, 0, 0, 0, 0, 0);
        }
    }

    private static IReadOnlyList<MatchDto> BuildFixtures()
    {
        var fixtures = new List<MatchDto>();
        fixtures.AddRange(BuildGroupFixtures());
        fixtures.AddRange(BuildKnockoutFixtures());
        return fixtures;
    }

    private static IEnumerable<MatchDto> BuildGroupFixtures()
    {
        var groups = new Dictionary<string, (TeamDto, TeamDto, TeamDto, TeamDto)>
        {
            ["A"] = (Team("USA", "United States", "USA"), Team("CAN", "Canada", "CAN"), Team("MEX", "Mexico", "MEX"), Team("JAM", "Jamaica", "JAM")),
            ["B"] = (Team("ENG", "England", "ENG"), Team("FRA", "France", "FRA"), Team("GER", "Germany", "GER"), Team("ESP", "Spain", "ESP")),
            ["C"] = (Team("BRA", "Brazil", "BRA"), Team("ARG", "Argentina", "ARG"), Team("URU", "Uruguay", "URU"), Team("COL", "Colombia", "COL")),
            ["D"] = (Team("POR", "Portugal", "POR"), Team("NED", "Netherlands", "NED"), Team("BEL", "Belgium", "BEL"), Team("CRO", "Croatia", "CRO")),
            ["E"] = (Team("ITA", "Italy", "ITA"), Team("SUI", "Switzerland", "SUI"), Team("SRB", "Serbia", "SRB"), Team("POL", "Poland", "POL")),
            ["F"] = (Team("MAR", "Morocco", "MAR"), Team("SEN", "Senegal", "SEN"), Team("GHA", "Ghana", "GHA"), Team("CMR", "Cameroon", "CMR")),
            ["G"] = (Team("JPN", "Japan", "JPN"), Team("KOR", "South Korea", "KOR"), Team("AUS", "Australia", "AUS"), Team("IRN", "Iran", "IRN")),
            ["H"] = (Team("ECU", "Ecuador", "ECU"), Team("PER", "Peru", "PER"), Team("CHI", "Chile", "CHI"), Team("PAR", "Paraguay", "PAR")),
        };

        var pairings = new (int Home, int Away)[] { (0, 1), (2, 3), (0, 2), (1, 3), (0, 3), (1, 2) };
        var dayOffset = -4;

        foreach (var (group, teams) in groups)
        {
            var teamList = new[] { teams.Item1, teams.Item2, teams.Item3, teams.Item4 };
            for (var i = 0; i < pairings.Length; i++)
            {
                var home = teamList[pairings[i].Home];
                var away = teamList[pairings[i].Away];
                var id = $"wc26-grp-{group}-{i + 1}";
                var kickoff = Now.AddDays(dayOffset + i);
                var finished = group == "A" && i < 2;

                yield return finished
                    ? Finished(id, home, away, kickoff, group, GroupVenue(group), i == 0 ? 2 : 1, i == 0 ? 1 : 0)
                    : Upcoming(id, home, away, kickoff, group, GroupVenue(group));
            }
        }
    }

    private static IEnumerable<MatchDto> BuildKnockoutFixtures()
    {
        yield return Upcoming("wc26-r16-01", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(10), "Round of 16", "", "AT&T Stadium, Dallas");
        yield return Upcoming("wc26-r16-02", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(10).AddHours(3), "Round of 16", "", "Mercedes-Benz Stadium, Atlanta");
        yield return Upcoming("wc26-r16-03", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(11), "Round of 16", "", "Lumen Field, Seattle");
        yield return Upcoming("wc26-r16-04", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(11).AddHours(3), "Round of 16", "", "BC Place, Vancouver");
        yield return Upcoming("wc26-r16-05", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(12), "Round of 16", "", "Levi's Stadium, San Francisco");
        yield return Upcoming("wc26-r16-06", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(12).AddHours(3), "Round of 16", "", "BMO Field, Toronto");
        yield return Upcoming("wc26-r16-07", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(13), "Round of 16", "", "NRG Stadium, Houston");
        yield return Upcoming("wc26-r16-08", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(13).AddHours(3), "Round of 16", "", "Lincoln Financial Field, Philadelphia");
        yield return Upcoming("wc26-qf-01", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(16), "Quarter-finals", "", "MetLife Stadium, New Jersey");
        yield return Upcoming("wc26-qf-02", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(16).AddHours(3), "Quarter-finals", "", "SoFi Stadium, Los Angeles");
        yield return Upcoming("wc26-qf-03", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(17), "Quarter-finals", "", "Hard Rock Stadium, Miami");
        yield return Upcoming("wc26-qf-04", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(17).AddHours(3), "Quarter-finals", "", "Estadio Azteca, Mexico City");
        yield return Upcoming("wc26-sf-01", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(20), "Semi-finals", "", "AT&T Stadium, Dallas");
        yield return Upcoming("wc26-sf-02", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(21), "Semi-finals", "", "Mercedes-Benz Stadium, Atlanta");
        yield return Upcoming("wc26-final", Team("TBD", "TBD", "TBD"), Team("TBD", "TBD", "TBD"), Now.AddDays(24), "Final", "", "MetLife Stadium, New Jersey");
    }

    private static string GroupVenue(string group) => group switch
    {
        "A" => "MetLife Stadium, New Jersey",
        "B" => "AT&T Stadium, Dallas",
        "C" => "SoFi Stadium, Los Angeles",
        "D" => "Hard Rock Stadium, Miami",
        "E" => "Mercedes-Benz Stadium, Atlanta",
        "F" => "Lumen Field, Seattle",
        "G" => "BC Place, Vancouver",
        _ => "BMO Field, Toronto",
    };

    private static MatchDto Finished(
        string id,
        TeamDto home,
        TeamDto away,
        DateTimeOffset kickoff,
        string group,
        string venue,
        int homeScore,
        int awayScore) =>
        new(id, home, away, kickoff, $"Group {group}", group, venue, "FT", homeScore, awayScore);

    private static MatchDto Upcoming(
        string id,
        TeamDto home,
        TeamDto away,
        DateTimeOffset kickoff,
        string group,
        string venue) =>
        new(id, home, away, kickoff, $"Group {group}", group, venue, "NS", null, null);

    private static MatchDto Upcoming(
        string id,
        TeamDto home,
        TeamDto away,
        DateTimeOffset kickoff,
        string stage,
        string group,
        string venue) =>
        new(id, home, away, kickoff, stage, group, venue, "NS", null, null);

    private static TeamDto Team(string id, string name, string code) =>
        new(id, name, code, code);

    private static StandingDto Standing(
        int rank,
        string teamId, string teamName, string teamCode,
        int played, int won, int drawn, int lost,
        int goalsFor, int goalsAgainst, int goalDiff, int points) =>
        new(rank, Team(teamId, teamName, teamCode), played, won, drawn, lost, goalsFor, goalsAgainst, goalDiff, points);

    public Task<IReadOnlyList<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        var teams = AllFixtures
            .SelectMany(m => new[] { m.HomeTeam, m.AwayTeam })
            .GroupBy(t => t.Code)
            .Select(g => g.First())
            .ToList();
        return Task.FromResult<IReadOnlyList<TeamDto>>(teams);
    }

    public Task<TeamSquadDto?> GetTeamSquadAsync(string teamProviderId, CancellationToken cancellationToken = default) =>
        Task.FromResult<TeamSquadDto?>(null);

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> GetAllStandingsAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = AllFixtures.Select(m => m.Group).Where(g => !string.IsNullOrWhiteSpace(g)).Distinct();
        var result = new Dictionary<string, IReadOnlyList<StandingDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            result[group] = await GetStandingsAsync(group, cancellationToken);
        }

        return result;
    }

    public Task<IReadOnlyList<MatchEventDto>> GetMatchEventsAsync(
        string matchId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MatchEventDto>>([]);

    public Task<IReadOnlyList<LineupPlayerDto>> GetMatchLineupsAsync(
        string matchId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LineupPlayerDto>>([]);
}
