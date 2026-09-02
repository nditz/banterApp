using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.SportsData.Dtos;
using BanterApp.Api.Services;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public class PremierLeagueMatchScopeTests
{
    private static Match MakeMatch(
        string id,
        Guid? seasonId = null,
        string group = "PL",
        string stage = "Regular Season - 1") =>
        new()
        {
            Id = id,
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            TeamACode = "ARS",
            TeamBCode = "CHE",
            KickoffTime = DateTimeOffset.UtcNow,
            Stage = stage,
            Group = group,
            Venue = "Emirates",
            Status = "NS",
            CompetitionSeasonId = seasonId,
            MatchweekNumber = 1
        };

    [Fact]
    public void IsPremierLeagueId_AcceptsPl26Only_NotBareApifb()
    {
        Assert.True(PremierLeagueMatchScope.IsPremierLeagueId("pl26-mw1-1"));
        Assert.False(PremierLeagueMatchScope.IsPremierLeagueId("apifb-12345"));
        Assert.False(PremierLeagueMatchScope.IsPremierLeagueId("of26-ko-1"));
        Assert.False(PremierLeagueMatchScope.IsPremierLeagueId(null));
    }

    [Fact]
    public void IsPremierLeague_RequiresSeasonGroupOrPl26_NotBareApifb()
    {
        Assert.False(PremierLeagueMatchScope.IsPremierLeague(
            MakeMatch("apifb-99", seasonId: null, group: "")));

        Assert.True(PremierLeagueMatchScope.IsPremierLeague(
            MakeMatch("apifb-99", seasonId: PremierLeagueCatalog.SeasonId, group: "PL")));

        Assert.True(PremierLeagueMatchScope.IsPremierLeague(
            MakeMatch("apifb-99", seasonId: null, group: "PL")));

        Assert.True(PremierLeagueMatchScope.IsPremierLeague(
            MakeMatch("pl26-mw1-1", seasonId: null, group: "")));
    }

    [Fact]
    public void IsPremierLeague_RejectsWorldCupShapedRowsEvenIfSeasonStamped()
    {
        Assert.False(PremierLeagueMatchScope.IsPremierLeague(
            MakeMatch(
                "apifb-1",
                seasonId: PremierLeagueCatalog.SeasonId,
                group: "A",
                stage: "Group A - 1")));

        Assert.False(PremierLeagueMatchScope.IsPremierLeague(
            MakeMatch(
                "of26-ko-1",
                seasonId: PremierLeagueCatalog.SeasonId,
                group: "PL",
                stage: "Regular Season - 1")));
    }

    [Fact]
    public void IsPremierLeagueDto_RequiresGroupPlOrPl26Id()
    {
        var team = new TeamDto("1", "Arsenal", "ARS", "ARS");
        var pl = new MatchDto("apifb-1", team, team, DateTimeOffset.UtcNow, "Regular Season - 1", "PL", "V", "NS", null, null, 1);
        var bare = new MatchDto("apifb-2", team, team, DateTimeOffset.UtcNow, "Group A - 1", "A", "V", "NS", null, null, null);
        var mock = new MatchDto("pl26-mw1-1", team, team, DateTimeOffset.UtcNow, "Regular Season - 1", "", "V", "NS", null, null, 1);

        Assert.True(PremierLeagueMatchScope.IsPremierLeagueDto(pl));
        Assert.False(PremierLeagueMatchScope.IsPremierLeagueDto(bare));
        Assert.True(PremierLeagueMatchScope.IsPremierLeagueDto(mock));
    }

    [Fact]
    public async Task WherePremierLeague_ExcludesBareApifbAndWorldCupRows()
    {
        await using var db = TestDbContextFactory.Create();
        db.Matches.AddRange(
            MakeMatch("pl26-mw1-1"),
            MakeMatch("apifb-pl", PremierLeagueCatalog.SeasonId, "PL"),
            MakeMatch("apifb-bare", null, ""),
            MakeMatch("apifb-wc", PremierLeagueCatalog.SeasonId, "A", "Group A - 1"),
            MakeMatch("of26-ko-1", PremierLeagueCatalog.SeasonId, "PL"));
        await db.SaveChangesAsync();

        var ids = await db.Matches.WherePremierLeague().Select(m => m.Id).OrderBy(x => x).ToListAsync();

        Assert.Equal(["apifb-pl", "pl26-mw1-1"], ids);
    }

    [Fact]
    public async Task WhereNonPremierLeague_IncludesMisStampedAndLegacy()
    {
        await using var db = TestDbContextFactory.Create();
        db.Matches.AddRange(
            MakeMatch("pl26-mw1-1"),
            MakeMatch("apifb-bare", null, ""),
            MakeMatch("apifb-wc", PremierLeagueCatalog.SeasonId, "A", "Group A - 1"),
            MakeMatch("wc26-1", null, "B", "Group B - 1"));
        await db.SaveChangesAsync();

        var ids = await db.Matches.WhereNonPremierLeague().Select(m => m.Id).OrderBy(x => x).ToListAsync();

        Assert.Equal(["apifb-bare", "apifb-wc", "wc26-1"], ids);
    }

    [Fact]
    public async Task CurrentMatchweek_ResolvesFromPremierLeagueCalendarOnly()
    {
        await using var db = TestDbContextFactory.Create();
        var now = new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);
        db.Matches.AddRange(
            new Match
            {
                Id = "apifb-wc",
                TeamA = "England",
                TeamB = "Brazil",
                TeamACode = "ENG",
                TeamBCode = "BRA",
                KickoffTime = now.AddDays(1),
                Stage = "Group A - 1",
                Group = "A",
                Venue = "MetLife",
                Status = "NS",
                CompetitionSeasonId = PremierLeagueCatalog.SeasonId,
                MatchweekNumber = 1
            },
            new Match
            {
                Id = "pl26-mw1-1",
                TeamA = "Arsenal",
                TeamB = "Coventry City",
                TeamACode = "ARS",
                TeamBCode = "COV",
                KickoffTime = now.AddDays(-6),
                Stage = "Regular Season - 1",
                Group = "PL",
                Venue = "Emirates",
                Status = "FT",
                CompetitionSeasonId = PremierLeagueCatalog.SeasonId,
                MatchweekNumber = 1,
                HomeScore = 3,
                AwayScore = 0
            },
            new Match
            {
                Id = "pl26-mw2-1",
                TeamA = "Liverpool",
                TeamB = "Nottingham Forest",
                TeamACode = "LIV",
                TeamBCode = "NFO",
                KickoffTime = now.AddDays(2),
                Stage = "Regular Season - 2",
                Group = "PL",
                Venue = "Anfield",
                Status = "NS",
                CompetitionSeasonId = PremierLeagueCatalog.SeasonId,
                MatchweekNumber = 2
            });
        await db.SaveChangesAsync();

        var rows = await db.Matches
            .WherePremierLeague()
            .Select(m => new { m.MatchweekNumber, m.Status, m.KickoffTime })
            .ToListAsync();

        var week = CurrentMatchweek.Resolve(
            rows.Select(m => (m.MatchweekNumber, (string?)m.Status, (DateTimeOffset?)m.KickoffTime)),
            now);

        Assert.Equal(2, week);
        Assert.DoesNotContain(rows, r => r.MatchweekNumber == 1 && r.Status == "NS");
    }
}
