namespace BanterApp.Api.Services;

/// <summary>
/// Shared dataset states for fixtures/standings APIs. Never collapse provider
/// failure into an empty list — callers must distinguish empty, stale, and error.
/// </summary>
public static class FootballDatasetStatus
{
    public const string Ok = "ok";
    public const string Empty = "empty";
    public const string Error = "error";
    public const string Stale = "stale";

    public static bool IsMockProvider(string? provider) =>
        !string.Equals(provider?.Trim(), "apifootball", StringComparison.OrdinalIgnoreCase);

    public static bool LooksLikeMockIds(IEnumerable<string> matchIds)
    {
        var ids = matchIds.ToList();
        return ids.Count > 0 &&
               ids.All(id => id.StartsWith("pl26-", StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasOverdueUnfinished(
        IEnumerable<(string? Status, DateTimeOffset Kickoff)> matches,
        DateTimeOffset now) =>
        matches.Any(m =>
            !CurrentMatchweek.IsFinished(m.Status) &&
            !CurrentMatchweek.IsLive(m.Status) &&
            m.Kickoff <= now);

    public static string FromFixtures(int matchCount, bool overdueUnfinished, bool providerFailed)
    {
        if (providerFailed)
        {
            return Error;
        }

        if (matchCount == 0)
        {
            return Empty;
        }

        return overdueUnfinished ? Stale : Ok;
    }
}
