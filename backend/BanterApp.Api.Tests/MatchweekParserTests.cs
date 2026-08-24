using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests;

public class MatchweekParserTests
{
    [Theory]
    [InlineData("Regular Season - 1", 1)]
    [InlineData("Regular Season - 38", 38)]
    [InlineData("Matchweek 12", 12)]
    [InlineData("Gameweek 4", 4)]
    [InlineData("Matchday 1", 1)]
    [InlineData("Matchday 38", 38)]
    public void TryParse_ReadsDomesticRoundLabels(string round, int expected)
    {
        Assert.Equal(expected, MatchweekParser.TryParse(round));
    }

    [Fact]
    public void TryParse_IgnoresOutOfRangeWeeks()
    {
        Assert.Null(MatchweekParser.TryParse("Regular Season - 99"));
    }
}
