namespace BanterApp.Api.Data.Entities;

public static class LeaderboardTypes
{
    public const string TopScorers = "top_scorers";
    public const string TopAssists = "top_assists";
    public const string BestPlayerRating = "best_player_rating";
    public const string MostMinutes = "most_minutes";
    public const string MostCards = "most_cards";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        TopScorers,
        TopAssists,
        BestPlayerRating,
        MostMinutes,
        MostCards
    };
}
