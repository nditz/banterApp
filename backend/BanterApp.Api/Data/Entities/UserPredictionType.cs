namespace BanterApp.Api.Data.Entities;

public static class UserPredictionTypes
{
    public const string WinnerCountry = "winner_country";
    public const string FinalistCountry = "finalist_country";
    public const string BestPlayer = "best_player";
    public const string TopGoalScorer = "top_goal_scorer";
    public const string TopAssistProvider = "top_assist_provider";
    public const string GoldenBoot = "golden_boot";
    public const string BestYoungPlayer = "best_young_player";
    public const string PlayerOfTournament = "player_of_tournament";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        WinnerCountry,
        FinalistCountry,
        BestPlayer,
        TopGoalScorer,
        TopAssistProvider,
        GoldenBoot,
        BestYoungPlayer,
        PlayerOfTournament
    };

    public static readonly HashSet<string> CountryTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        WinnerCountry,
        FinalistCountry
    };

    public static readonly HashSet<string> PlayerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        BestPlayer,
        TopGoalScorer,
        TopAssistProvider,
        GoldenBoot,
        BestYoungPlayer,
        PlayerOfTournament
    };

    public static bool RequiresCountry(string type) => CountryTypes.Contains(type);

    public static bool RequiresPlayer(string type) => PlayerTypes.Contains(type);
}
