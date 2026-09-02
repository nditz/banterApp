using System.Globalization;
using BanterApp.Api.Integrations.Media;
using Xunit;

namespace BanterApp.Api.Tests;

public class GameweekGifWindowTests
{
    [Fact]
    public void FridayThroughMonday_ShareOneWindow()
    {
        var friday = Uk("2026-09-04 10:00");
        var saturday = Uk("2026-09-05 18:00");
        var sunday = Uk("2026-09-06 01:00");
        var mondayNight = Uk("2026-09-07 23:30");

        var weekend = GameweekGifWindow.For(friday);
        Assert.Equal("2026-09-04", weekend.Id);
        Assert.Equal(weekend.Id, GameweekGifWindow.For(saturday).Id);
        Assert.Equal(weekend.Id, GameweekGifWindow.For(sunday).Id);
        Assert.Equal(weekend.Id, GameweekGifWindow.For(mondayNight).Id);
        Assert.True(mondayNight < weekend.EndUtc);
    }

    [Fact]
    public void TuesdayStartsANewWindow()
    {
        var monday = Uk("2026-09-07 23:59");
        var tuesday = Uk("2026-09-08 00:01");

        var weekend = GameweekGifWindow.For(monday);
        var midweek = GameweekGifWindow.For(tuesday);

        Assert.Equal("2026-09-04", weekend.Id);
        Assert.Equal("2026-09-08", midweek.Id);
        Assert.NotEqual(weekend.Id, midweek.Id);
    }

    [Fact]
    public void NextFridayStartsAFreshWeekendWindow()
    {
        var thursday = Uk("2026-09-03 12:00");
        var friday = Uk("2026-09-04 00:00");

        Assert.Equal("2026-09-01", GameweekGifWindow.For(thursday).Id);
        Assert.Equal("2026-09-04", GameweekGifWindow.For(friday).Id);
    }

    private static DateTimeOffset Uk(string local)
    {
        var tz = TimeZoneInfo.TryFindSystemTimeZoneById("Europe/London", out var london)
            ? london
            : TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var parsed = DateTime.Parse(local, CultureInfo.InvariantCulture);
        var utc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified),
            tz);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
