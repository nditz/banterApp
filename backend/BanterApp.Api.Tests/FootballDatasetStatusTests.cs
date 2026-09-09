using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests;

public class FootballDatasetStatusTests
{
    [Fact]
    public void FromFixtures_Empty_IsEmptyNotError()
    {
        Assert.Equal(FootballDatasetStatus.Empty, FootballDatasetStatus.FromFixtures(0, false, false));
    }

    [Fact]
    public void FromFixtures_ProviderFailed_IsErrorEvenIfRowsExist()
    {
        Assert.Equal(FootballDatasetStatus.Error, FootballDatasetStatus.FromFixtures(10, false, true));
    }

    [Fact]
    public void FromFixtures_OverdueUnfinished_IsStale()
    {
        Assert.Equal(FootballDatasetStatus.Stale, FootballDatasetStatus.FromFixtures(10, true, false));
    }

    [Fact]
    public void LooksLikeMockIds_DetectsPl26Prefix()
    {
        Assert.True(FootballDatasetStatus.LooksLikeMockIds(["pl26-mw2-1", "pl26-mw2-2"]));
        Assert.False(FootballDatasetStatus.LooksLikeMockIds(["apifb-123"]));
        Assert.False(FootballDatasetStatus.LooksLikeMockIds([]));
    }

    [Fact]
    public void HasOverdueUnfinished_IgnoresFinishedAndLive()
    {
        var now = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
        Assert.False(FootballDatasetStatus.HasOverdueUnfinished(
        [
            ("FT", now.AddDays(-10)),
            ("LIVE", now.AddHours(-1)),
            ("NS", now.AddDays(2)),
        ], now));
        Assert.True(FootballDatasetStatus.HasOverdueUnfinished(
        [
            ("NS", now.AddDays(-2)),
        ], now));
    }
}
