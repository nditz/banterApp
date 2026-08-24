namespace BanterApp.Api.Services;

/// <summary>
/// Current Premier League matchweek: the lowest round that still has an unfinished fixture,
/// matching how BBC Sport and the Premier League site keep the active gameweek open.
/// </summary>
public static class CurrentMatchweek
{
    public static bool IsFinished(string? status) =>
        status is "FT" or "AET" or "PEN" or "WO";

    public static int Resolve(IEnumerable<(int? Number, string? Status)> matches)
    {
        var numbered = matches
            .Where(m => m.Number is >= 1 and <= 38)
            .Select(m => (Number: m.Number!.Value, m.Status))
            .ToList();

        if (numbered.Count == 0)
        {
            return 1;
        }

        var open = numbered
            .Where(m => !IsFinished(m.Status))
            .Select(m => m.Number)
            .ToList();

        return open.Count > 0 ? open.Min() : numbered.Max(m => m.Number);
    }
}
