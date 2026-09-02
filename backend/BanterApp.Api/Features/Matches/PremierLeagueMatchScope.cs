using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Features.Matches;

/// <summary>
/// Current-product match rows are Premier League 2026/27.
/// World Cup leftovers (<c>of26-*</c>, <c>wc26-*</c>) and mis-stamped foreign
/// fixtures are purged on boot; these filters remain as defense in depth.
/// </summary>
public static class PremierLeagueMatchScope
{
    /// <summary>
    /// Known mock / seed Premier League fixture ids only.
    /// Bare <c>apifb-*</c> is <em>not</em> sufficient — those rows must also carry
    /// the PL season id and/or <c>Group == "PL"</c>.
    /// </summary>
    public static bool IsPremierLeagueId(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        id.StartsWith("pl26-", StringComparison.OrdinalIgnoreCase);

    public static bool IsWorldCupLegacyId(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        (id.StartsWith("of26-", StringComparison.OrdinalIgnoreCase) ||
         id.StartsWith("wc26-", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Provider DTOs that are safe to upsert as Premier League fixtures.
    /// Requires explicit PL group or known <c>pl26-*</c> seed ids.
    /// </summary>
    public static bool IsPremierLeagueDto(MatchDto dto) =>
        IsPremierLeagueId(dto.Id) ||
        string.Equals(dto.Group, "PL", StringComparison.OrdinalIgnoreCase);

    public static bool LooksLikeWorldCupFixture(Match match)
    {
        if (IsWorldCupLegacyId(match.Id))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(match.Group) &&
            match.Group.Length == 1 &&
            match.Group[0] is >= 'A' and <= 'L')
        {
            return true;
        }

        var stage = match.Stage ?? string.Empty;
        if (stage.Contains("World Cup", StringComparison.OrdinalIgnoreCase) ||
            stage.Contains("Round of", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // API-Football WC rounds look like "Group A - 1"; PL rounds are "Regular Season - N".
        return stage.StartsWith("Group ", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPremierLeague(Match match) =>
        !LooksLikeWorldCupFixture(match) &&
        (match.CompetitionSeasonId == PremierLeagueCatalog.SeasonId ||
         IsPremierLeagueId(match.Id) ||
         string.Equals(match.Group, "PL", StringComparison.OrdinalIgnoreCase));

    public static IQueryable<Match> WherePremierLeague(this IQueryable<Match> query) =>
        query.Where(m =>
            !m.Id.StartsWith("of26-") &&
            !m.Id.StartsWith("wc26-") &&
            (m.Group == null || m.Group == "" || m.Group == "PL") &&
            !m.Stage.StartsWith("Group ") &&
            !m.Stage.Contains("World Cup") &&
            !m.Stage.Contains("Round of") &&
            (m.CompetitionSeasonId == PremierLeagueCatalog.SeasonId ||
             m.Id.StartsWith("pl26-") ||
             m.Group == "PL"));

    /// <summary>
    /// Rows that must be removed so the product never surfaces non-PL fixtures.
    /// Includes legacy WC ids, WC-shaped stages/groups, and anything that fails
    /// the tightened Premier League filter.
    /// </summary>
    public static IQueryable<Match> WhereNonPremierLeague(this IQueryable<Match> query) =>
        query.Where(m =>
            m.Id.StartsWith("of26-") ||
            m.Id.StartsWith("wc26-") ||
            (m.Group != null && m.Group != "" && m.Group != "PL") ||
            m.Stage.StartsWith("Group ") ||
            m.Stage.Contains("World Cup") ||
            m.Stage.Contains("Round of") ||
            !(m.CompetitionSeasonId == PremierLeagueCatalog.SeasonId ||
              m.Id.StartsWith("pl26-") ||
              m.Group == "PL"));

    /// <summary>OpenFootball / mock World Cup fixture ids.</summary>
    public static IQueryable<Match> WhereWorldCupLegacy(this IQueryable<Match> query) =>
        query.Where(m =>
            m.Id.StartsWith("of26-") ||
            m.Id.StartsWith("wc26-"));
}
