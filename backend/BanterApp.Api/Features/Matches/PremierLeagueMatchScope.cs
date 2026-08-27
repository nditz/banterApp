using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Matches;

/// <summary>
/// Current-product match rows are Premier League 2026/27.
/// World Cup leftovers (<c>of26-*</c>, <c>wc26-*</c>) are purged on boot;
/// these filters remain as defense in depth.
/// </summary>
public static class PremierLeagueMatchScope
{
    public static bool IsPremierLeagueId(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        (id.StartsWith("apifb-", StringComparison.OrdinalIgnoreCase) ||
         id.StartsWith("pl26-", StringComparison.OrdinalIgnoreCase));

    public static bool IsWorldCupLegacyId(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        (id.StartsWith("of26-", StringComparison.OrdinalIgnoreCase) ||
         id.StartsWith("wc26-", StringComparison.OrdinalIgnoreCase));

    public static bool IsPremierLeague(Match match) =>
        match.CompetitionSeasonId == PremierLeagueCatalog.SeasonId ||
        IsPremierLeagueId(match.Id) ||
        string.Equals(match.Group, "PL", StringComparison.OrdinalIgnoreCase);

    public static IQueryable<Match> WherePremierLeague(this IQueryable<Match> query) =>
        query.Where(m =>
            m.CompetitionSeasonId == PremierLeagueCatalog.SeasonId ||
            m.Id.StartsWith("apifb-") ||
            m.Id.StartsWith("pl26-") ||
            m.Group == "PL");

    /// <summary>OpenFootball / mock World Cup fixture ids.</summary>
    public static IQueryable<Match> WhereWorldCupLegacy(this IQueryable<Match> query) =>
        query.Where(m =>
            m.Id.StartsWith("of26-") ||
            m.Id.StartsWith("wc26-"));
}
