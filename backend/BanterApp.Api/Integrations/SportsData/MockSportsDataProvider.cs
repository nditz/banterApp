using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Integrations.SportsData;

public sealed class MockSportsDataProvider : ISportsDataProvider, ISportsDataEnrichment
{
    private static readonly TimeZoneInfo UkTimeZone =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/London", out var london)
            ? london
            : TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

    private static readonly IReadOnlyList<MatchDto> AllFixtures = BuildFixtures();

    private static readonly Dictionary<string, MatchStatisticsDto> Statistics = new()
    {
        ["pl26-mw1-1"] = new("pl26-mw1-1", 54, 46, 14, 9, 6, 3, 7, 4, 11, 13, 2, 3, 0, 0),
        ["pl26-mw1-2"] = new("pl26-mw1-2", 61, 39, 18, 7, 9, 2, 5, 2, 8, 14, 1, 2, 0, 0),
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
        var matches = AllFixtures;
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

        foreach (var match in matches)
        {
            Ensure(teams, match.HomeTeam);
            Ensure(teams, match.AwayTeam);
        }

        foreach (var match in matches.Where(m => m.Status == "FT"))
        {
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
            .ThenByDescending(t => t.Gf)
            .ThenBy(t => t.Team.Name, StringComparer.OrdinalIgnoreCase)
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
        var ars = Team("ARS", "Arsenal", "ARS");
        var avl = Team("AVL", "Aston Villa", "AVL");
        var bou = Team("BOU", "Bournemouth", "BOU");
        var bre = Team("BRE", "Brentford", "BRE");
        var bha = Team("BHA", "Brighton & Hove Albion", "BHA");
        var che = Team("CHE", "Chelsea", "CHE");
        var cov = Team("COV", "Coventry City", "COV");
        var cry = Team("CRY", "Crystal Palace", "CRY");
        var eve = Team("EVE", "Everton", "EVE");
        var ful = Team("FUL", "Fulham", "FUL");
        var hul = Team("HUL", "Hull City", "HUL");
        var ips = Team("IPS", "Ipswich Town", "IPS");
        var lee = Team("LEE", "Leeds United", "LEE");
        var liv = Team("LIV", "Liverpool", "LIV");
        var mci = Team("MCI", "Manchester City", "MCI");
        var mun = Team("MUN", "Manchester United", "MUN");
        var neu = Team("NEW", "Newcastle United", "NEW");
        var nfo = Team("NFO", "Nottingham Forest", "NFO");
        var sun = Team("SUN", "Sunderland", "SUN");
        var tot = Team("TOT", "Tottenham Hotspur", "TOT");

        // Matchweeks 1–2 follow the published 2026/27 opening rounds (BBC Sport / Premier League).
        // Weeks 3–4 are a plausible local/dev continuation so current-week resolution can move past
        // 31 Aug 2026. They are not official fixtures — APIs must set official=false for mock data.
        var week1 = new (TeamDto Home, TeamDto Away, int Year, int Month, int Day, int Hour, int Minute, string Venue, int Hs, int As)[]
        {
            (ars, cov, 2026, 8, 21, 20, 0, "Emirates Stadium", 3, 0),
            (hul, mun, 2026, 8, 22, 12, 30, "MKM Stadium", 2, 0),
            (eve, cry, 2026, 8, 22, 15, 0, "Hill Dickinson Stadium", 2, 0),
            (ips, sun, 2026, 8, 22, 15, 0, "Portman Road", 2, 1),
            (nfo, lee, 2026, 8, 22, 15, 0, "City Ground", 0, 1),
            (bre, tot, 2026, 8, 22, 17, 30, "Gtech Community Stadium", 3, 0),
            (bha, avl, 2026, 8, 23, 14, 0, "American Express Stadium", 4, 0),
            (mci, bou, 2026, 8, 23, 14, 0, "Etihad Stadium", 2, 1),
            (neu, liv, 2026, 8, 23, 16, 30, "St James' Park", 2, 2),
            (ful, che, 2026, 8, 24, 20, 0, "Craven Cottage", 0, 2),
        };

        var week2 = new (TeamDto Home, TeamDto Away, int Year, int Month, int Day, int Hour, int Minute, string Venue, int Hs, int As)[]
        {
            (cry, mci, 2026, 8, 28, 20, 0, "Selhurst Park", 1, 2),
            (liv, nfo, 2026, 8, 29, 12, 30, "Anfield", 3, 0),
            (bou, eve, 2026, 8, 29, 15, 0, "Vitality Stadium", 1, 1),
            (cov, hul, 2026, 8, 29, 15, 0, "Coventry Building Society Arena", 0, 2),
            (tot, neu, 2026, 8, 29, 17, 30, "Tottenham Hotspur Stadium", 2, 1),
            (che, bha, 2026, 8, 30, 14, 0, "Stamford Bridge", 2, 1),
            (lee, bre, 2026, 8, 30, 14, 0, "Elland Road", 1, 1),
            (sun, ful, 2026, 8, 30, 14, 0, "Stadium of Light", 0, 1),
            (mun, ips, 2026, 8, 30, 16, 30, "Old Trafford", 1, 0),
            (avl, ars, 2026, 8, 31, 20, 0, "Villa Park", 1, 2),
        };

        var week3 = new (TeamDto Home, TeamDto Away, int Year, int Month, int Day, int Hour, int Minute, string Venue)[]
        {
            (liv, ars, 2026, 9, 12, 12, 30, "Anfield"),
            (avl, eve, 2026, 9, 12, 15, 0, "Villa Park"),
            (nfo, bre, 2026, 9, 12, 15, 0, "City Ground"),
            (lee, ips, 2026, 9, 12, 15, 0, "Elland Road"),
            (cry, ful, 2026, 9, 12, 15, 0, "Selhurst Park"),
            (bou, cov, 2026, 9, 12, 15, 0, "Vitality Stadium"),
            (sun, hul, 2026, 9, 12, 15, 0, "Stadium of Light"),
            (mci, che, 2026, 9, 12, 17, 30, "Etihad Stadium"),
            (tot, bha, 2026, 9, 13, 14, 0, "Tottenham Hotspur Stadium"),
            (mun, neu, 2026, 9, 13, 16, 30, "Old Trafford"),
        };

        var week4 = new (TeamDto Home, TeamDto Away, int Year, int Month, int Day, int Hour, int Minute, string Venue)[]
        {
            (ars, mci, 2026, 9, 19, 12, 30, "Emirates Stadium"),
            (che, liv, 2026, 9, 19, 15, 0, "Stamford Bridge"),
            (neu, avl, 2026, 9, 19, 15, 0, "St James' Park"),
            (bha, mun, 2026, 9, 19, 15, 0, "American Express Stadium"),
            (eve, tot, 2026, 9, 19, 15, 0, "Hill Dickinson Stadium"),
            (bre, lee, 2026, 9, 19, 15, 0, "Gtech Community Stadium"),
            (ips, nfo, 2026, 9, 19, 15, 0, "Portman Road"),
            (ful, bou, 2026, 9, 19, 17, 30, "Craven Cottage"),
            (cov, sun, 2026, 9, 20, 14, 0, "Coventry Building Society Arena"),
            (hul, cry, 2026, 9, 20, 16, 30, "MKM Stadium"),
        };

        var fixtures = new List<MatchDto>();
        AppendFinished(fixtures, week1, 1);
        AppendFinished(fixtures, week2, 2);
        AppendUpcoming(fixtures, week3, 3);
        AppendUpcoming(fixtures, week4, 4);
        return fixtures;
    }

    private static void AppendFinished(
        List<MatchDto> fixtures,
        (TeamDto Home, TeamDto Away, int Year, int Month, int Day, int Hour, int Minute, string Venue, int Hs, int As)[] week,
        int matchweek)
    {
        for (var i = 0; i < week.Length; i++)
        {
            var (home, away, y, mo, d, h, mi, venue, hs, ascore) = week[i];
            fixtures.Add(Finished(
                $"pl26-mw{matchweek}-{i + 1}",
                home,
                away,
                KickoffUk(y, mo, d, h, mi),
                matchweek,
                venue,
                hs,
                ascore));
        }
    }

    private static void AppendUpcoming(
        List<MatchDto> fixtures,
        (TeamDto Home, TeamDto Away, int Year, int Month, int Day, int Hour, int Minute, string Venue)[] week,
        int matchweek)
    {
        for (var i = 0; i < week.Length; i++)
        {
            var (home, away, y, mo, d, h, mi, venue) = week[i];
            fixtures.Add(Upcoming(
                $"pl26-mw{matchweek}-{i + 1}",
                home,
                away,
                KickoffUk(y, mo, d, h, mi),
                matchweek,
                venue));
        }
    }

    /// <summary>
    /// Premier League kickoffs are published in UK local time. Npgsql requires UTC
    /// (offset 0) for PostgreSQL timestamptz, so convert before the DTO is stored.
    /// </summary>
    private static DateTimeOffset KickoffUk(int year, int month, int day, int hour, int minute)
    {
        var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, UkTimeZone);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static MatchDto Finished(
        string id,
        TeamDto home,
        TeamDto away,
        DateTimeOffset kickoff,
        int matchweek,
        string venue,
        int homeScore,
        int awayScore) =>
        new(id, home, away, kickoff, $"Regular Season - {matchweek}", "PL", venue, "FT", homeScore, awayScore, matchweek);

    private static MatchDto Upcoming(
        string id,
        TeamDto home,
        TeamDto away,
        DateTimeOffset kickoff,
        int matchweek,
        string venue) =>
        new(id, home, away, kickoff, $"Regular Season - {matchweek}", "PL", venue, "NS", null, null, matchweek);

    private static TeamDto Team(string id, string name, string code) =>
        new(id, name, code, code, ClubBadges.UrlFor(code, name));

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
        var groups = new[] { "PL" };
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
