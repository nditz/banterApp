using BanterApp.Api.Features.Matches;
using Xunit;

namespace BanterApp.Api.Tests;

public class PremierLeagueTableRankingTests
{
    [Fact]
    public void RanksByPointsThenGoalDifferenceThenGoalsScored()
    {
        var ranked = PremierLeagueTableRanking.Rank(
        [
            Row("CHE", "Chelsea", points: 10, gd: 4, gf: 12),
            Row("ARS", "Arsenal", points: 10, gd: 5, gf: 11),
            Row("MCI", "Manchester City", points: 13, gd: 8, gf: 16),
            Row("LIV", "Liverpool", points: 10, gd: 4, gf: 14),
        ]);

        Assert.Equal(["MCI", "ARS", "LIV", "CHE"], ranked.Select(r => r.TeamCode).ToArray());
        Assert.Equal([1, 2, 3, 4], ranked.Select(r => r.Rank).ToArray());
    }

    [Fact]
    public void UsesTeamNameWhenPointsGoalDifferenceAndGoalsAreLevel()
    {
        var ranked = PremierLeagueTableRanking.Rank(
        [
            Row("BRE", "Brentford", points: 3, gd: 3, gf: 3),
            Row("ARS", "Arsenal", points: 3, gd: 3, gf: 3),
        ]);

        Assert.Equal(["ARS", "BRE"], ranked.Select(r => r.TeamCode).ToArray());
    }

    [Fact]
    public void DropsDuplicateTeamCodes()
    {
        var ranked = PremierLeagueTableRanking.Rank(
        [
            Row("ARS", "Arsenal", points: 9, gd: 4, gf: 8),
            Row("ARS", "Arsenal", points: 6, gd: 1, gf: 5),
            Row("CHE", "Chelsea", points: 7, gd: 2, gf: 6),
        ]);

        Assert.Equal(2, ranked.Count);
        Assert.Equal("ARS", ranked[0].TeamCode);
        Assert.Equal(1, ranked[0].Rank);
    }

    private static StandingRowResponse Row(string code, string name, int points, int gd, int gf) =>
        new(0, code, name, null, 5, 2, 1, 2, gf, gf - gd, gd, points);
}
