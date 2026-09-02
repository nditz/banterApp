namespace BanterApp.Api.Services;

/// <summary>
/// Current Premier League matchweek: the lowest round that still has an unfinished fixture,
/// matching how BBC Sport and the Premier League site keep the active gameweek open.
/// When kickoff times are supplied, a week whose remaining fixtures are already in the past
/// no longer blocks the next round of open picks.
/// </summary>
public static class CurrentMatchweek
{
    public static bool IsFinished(string? status) =>
        status is "FT" or "AET" or "PEN" or "WO" or "CANC" or "ABD";

    public static bool IsLive(string? status) =>
        status is "LIVE" or "1H" or "2H" or "HT" or "ET" or "BT" or "P" or "INT" or "SUSP";

    public static int Resolve(IEnumerable<(int? Number, string? Status)> matches) =>
        Resolve(
            matches.Select(m => (m.Number, m.Status, (DateTimeOffset?)null)),
            DateTimeOffset.UtcNow);

    public static int Resolve(
        IEnumerable<(int? Number, string? Status, DateTimeOffset? Kickoff)> matches,
        DateTimeOffset now)
    {
        var numbered = matches
            .Where(m => m.Number is >= 1 and <= 38)
            .Select(m => (Number: m.Number!.Value, m.Status, m.Kickoff))
            .ToList();

        if (numbered.Count == 0)
        {
            return 1;
        }

        var playable = numbered
            .Where(m => !IsFinished(m.Status) &&
                        (IsLive(m.Status) || m.Kickoff is null || m.Kickoff > now))
            .Select(m => m.Number)
            .ToList();

        if (playable.Count > 0)
        {
            return playable.Min();
        }

        var unfinished = numbered
            .Where(m => !IsFinished(m.Status))
            .Select(m => m.Number)
            .ToList();

        // Kickoffs are all in the past but results never landed: stay on the latest
        // open round instead of jumping back to an earlier unfinished week.
        return unfinished.Count > 0 ? unfinished.Max() : numbered.Max(m => m.Number);
    }
}
