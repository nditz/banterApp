namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Premier League weekend uniqueness window: Friday 00:00 through Tuesday 00:00 UK time
/// (Monday inclusive). Tuesday–Thursday use a separate midweek window so the next Friday
/// starts with a clean slate.
/// </summary>
public sealed class GameweekGifWindow
{
    private static readonly TimeZoneInfo UkTimeZone =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/London", out var london)
            ? london
            : TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

    public GameweekGifWindow(string id, DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        Id = id;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public string Id { get; }

    public DateTimeOffset StartUtc { get; }

    public DateTimeOffset EndUtc { get; }

    public static GameweekGifWindow Current() => For(DateTimeOffset.UtcNow);

    public static GameweekGifWindow For(DateTimeOffset utcNow)
    {
        var uk = TimeZoneInfo.ConvertTime(utcNow, UkTimeZone);
        var date = DateOnly.FromDateTime(uk.DateTime);
        var (startDate, endDate) = date.DayOfWeek switch
        {
            DayOfWeek.Friday => (date, date.AddDays(4)),
            DayOfWeek.Saturday => (date.AddDays(-1), date.AddDays(3)),
            DayOfWeek.Sunday => (date.AddDays(-2), date.AddDays(2)),
            DayOfWeek.Monday => (date.AddDays(-3), date.AddDays(1)),
            DayOfWeek.Tuesday => (date, date.AddDays(3)),
            DayOfWeek.Wednesday => (date.AddDays(-1), date.AddDays(2)),
            _ => (date.AddDays(-2), date.AddDays(1)), // Thursday → Tue–Fri
        };

        return new GameweekGifWindow(
            startDate.ToString("yyyy-MM-dd"),
            ToUkMidnightUtc(startDate),
            ToUkMidnightUtc(endDate));
    }

    private static DateTimeOffset ToUkMidnightUtc(DateOnly date)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, UkTimeZone);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
