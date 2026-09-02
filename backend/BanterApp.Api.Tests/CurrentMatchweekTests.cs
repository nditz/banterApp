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
    public void Resolve_SkipsPastUnfinishedWeeksWhenKickoffIsKnown()
    {
        var now = new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);
        var week = CurrentMatchweek.Resolve(
        [
            (1, "FT", now.AddDays(-6)),
            (1, "NS", now.AddDays(-3)),
            (2, "NS", now.AddDays(1)),
        ], now);

        Assert.Equal(2, week);
    }

    [Fact]
    public void Resolve_WhenEveryRemainingKickoffIsInThePast_UsesLatestUnfinishedRound()
    {
        var now = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
        var week = CurrentMatchweek.Resolve(
        [
            (1, "FT", now.AddDays(-12)),
            (1, "NS", now.AddDays(-9)),
            (2, "NS", now.AddDays(-4)),
            (2, "NS", now.AddDays(-2)),
        ], now);

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
