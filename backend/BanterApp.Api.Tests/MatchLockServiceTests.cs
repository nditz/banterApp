using BanterApp.Api.Data.Entities;
using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests;

public class MatchLockServiceTests
{
    [Fact]
    public void IsLocked_BeforeKickoff_WhenNotStarted()
    {
        var match = Fixture("NS", DateTimeOffset.UtcNow.AddHours(3));
        Assert.False(MatchLockService.IsLocked(match, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsLocked_AfterKickoff_EvenIfStatusIsNotStarted()
    {
        var match = Fixture("NS", DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.True(MatchLockService.IsLocked(match, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsLocked_WhenFinished_EvenIfKickoffIsInTheFuture()
    {
        var match = Fixture("FT", DateTimeOffset.UtcNow.AddDays(1));
        Assert.True(MatchLockService.IsLocked(match, DateTimeOffset.UtcNow));
    }

    private static Match Fixture(string status, DateTimeOffset kickoff) => new()
    {
        Id = "lock-test",
        TeamA = "Home",
        TeamB = "Away",
        Status = status,
        KickoffTime = kickoff
    };
}
