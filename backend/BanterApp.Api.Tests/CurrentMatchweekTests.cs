using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests;

public class CurrentMatchweekTests
{
    [Fact]
    public void Resolve_Empty_DefaultsToMatchweekOne()
    {
        Assert.Equal(1, CurrentMatchweek.Resolve([]));
    }

    [Fact]
    public void Resolve_UsesLowestRoundWithAnUnfinishedFixture()
    {
        var week = CurrentMatchweek.Resolve(
        [
            (1, "FT"),
            (1, "FT"),
            (2, "NS"),
            (2, "FT"),
        ]);

        Assert.Equal(2, week);
    }

    [Fact]
    public void Resolve_AfterEveryFixtureIsFinished_UsesTheLatestRound()
    {
        var week = CurrentMatchweek.Resolve(
        [
            (1, "FT"),
            (2, "AET"),
            (2, "PEN"),
        ]);

        Assert.Equal(2, week);
    }
}
