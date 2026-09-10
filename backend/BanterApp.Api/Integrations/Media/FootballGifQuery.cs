using BanterApp.Api.Data;

namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Football-biased GIF search phrases. Used to seed our query store and to filter Giphy
/// trending-search terms so the timeline does not lock onto generic viral GIFs.
/// </summary>
public static class FootballGifQuery
{
    public static readonly IReadOnlyList<string> CuratedPhrases =
    [
        "premier league",
        "premier league meme",
        "epl celebration",
        "football meme",
        "soccer celebration",
        "goal celebration",
        "last minute winner",
        "var check football",
        "red card football",
        "shocked football fan",
        "football roast meme",
        "trophy lift football",
        "arsenal celebration",
        "liverpool anfield celebration",
        "manchester city goal",
        "saka celebration",
        "salah celebration",
        "haaland celebration",
        "gary neville pundit",
        "sky sports super sunday",
        "monday night football",
        "premier league derby",
        "last minute winner premier league",
        "var controversy premier league",
        "red card premier league",
        "football fan facepalm",
        "pundit shocked reaction",
        "premier league title race",
        "big six premier league",
        "clean sheet celebration",
        "hat trick celebration",
        "own goal football",
        "penalty miss football",
        "crowd eruption stadium",
        "roy keane angry pundit",
        "mourinho press conference",
        "premier league table",
        "matchday chaos football",
        "late drama premier league",
        "tottenham spurs celebration"
    ];

    private static readonly string[] FootballTokens =
    [
        "football", "soccer", "premier", "epl", "goal", "offside", "referee", "var",
        "celebration", "stadium", "matchday", "kickoff", "hat-trick", "hat trick",
        "red card", "yellow card", "transfer", "derby", "champions league",
        "ucl", "europa", "arsenal", "chelsea", "liverpool", "united", "spurs",
        "tottenham", "newcastle", "brighton", "villa", "hammers", "palace", "everton",
        "wolves", "fulham", "brentford", "bournemouth", "forest", "burnley", "leeds",
        "sunderland", "man city", "manchester", "haaland", "salah", "saka",
    ];

    public static bool LooksFootball(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase) || CompetitionFocus.LooksLikeOffFocus(phrase))
        {
            return false;
        }

        var value = phrase.Trim();
        foreach (var token in FootballTokens)
        {
            if (value.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
