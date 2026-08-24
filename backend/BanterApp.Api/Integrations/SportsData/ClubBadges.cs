namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Premier League club crests from API-Football, used when a provider omits a logo.
/// </summary>
public static class ClubBadges
{
    private static readonly Dictionary<string, string> IdsByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ARS"] = "42",
        ["AVL"] = "66",
        ["BOU"] = "35",
        ["BRE"] = "55",
        ["BHA"] = "51",
        ["BRI"] = "51",
        ["BUR"] = "44",
        ["CHE"] = "49",
        ["COV"] = "71",
        ["CRY"] = "52",
        ["EVE"] = "45",
        ["FUL"] = "36",
        ["HUL"] = "64",
        ["IPS"] = "57",
        ["LEE"] = "63",
        ["LEI"] = "46",
        ["LIV"] = "40",
        ["MAC"] = "50",
        ["MCI"] = "50",
        ["MUN"] = "33",
        ["NEW"] = "34",
        ["NFO"] = "65",
        ["NOT"] = "65",
        ["SOU"] = "41",
        ["SUN"] = "746",
        ["TOT"] = "47",
        ["WHU"] = "48",
        ["WOL"] = "39",
    };

    private static readonly Dictionary<string, string> IdsByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arsenal"] = "42",
        ["Aston Villa"] = "66",
        ["Bournemouth"] = "35",
        ["AFC Bournemouth"] = "35",
        ["Brentford"] = "55",
        ["Brighton"] = "51",
        ["Brighton & Hove Albion"] = "51",
        ["Burnley"] = "44",
        ["Chelsea"] = "49",
        ["Coventry"] = "71",
        ["Coventry City"] = "71",
        ["Crystal Palace"] = "52",
        ["Everton"] = "45",
        ["Fulham"] = "36",
        ["Hull"] = "64",
        ["Hull City"] = "64",
        ["Ipswich"] = "57",
        ["Ipswich Town"] = "57",
        ["Leeds"] = "63",
        ["Leeds United"] = "63",
        ["Leicester"] = "46",
        ["Leicester City"] = "46",
        ["Liverpool"] = "40",
        ["Manchester City"] = "50",
        ["Man City"] = "50",
        ["Manchester United"] = "33",
        ["Man United"] = "33",
        ["Newcastle"] = "34",
        ["Newcastle United"] = "34",
        ["Nottingham Forest"] = "65",
        ["Nott'm Forest"] = "65",
        ["Southampton"] = "41",
        ["Sunderland"] = "746",
        ["Tottenham"] = "47",
        ["Tottenham Hotspur"] = "47",
        ["West Ham"] = "48",
        ["West Ham United"] = "48",
        ["Wolves"] = "39",
        ["Wolverhampton Wanderers"] = "39",
    };

    public static string? UrlFor(string? code, string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(code) && IdsByCode.TryGetValue(code.Trim(), out var byCode))
        {
            return Url(byCode);
        }

        if (!string.IsNullOrWhiteSpace(name) && IdsByName.TryGetValue(name.Trim(), out var byName))
        {
            return Url(byName);
        }

        return null;
    }

    public static string? Coalesce(string? logoUrl, string? code, string? name = null) =>
        string.IsNullOrWhiteSpace(logoUrl) ? UrlFor(code, name) : logoUrl;

    private static string Url(string teamId) =>
        $"https://media.api-sports.io/football/teams/{teamId}.png";
}
