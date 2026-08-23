namespace BanterApp.Api.Data.Entities;

public static class UserPredictionTypes
{
    public const string LeagueWinner = "league_winner";
    public const string TopFour = "top_four";
    public const string Relegated = "relegated";
    public const string BestPlayer = "best_player";
    public const string TopGoalScorer = "top_goal_scorer";
    public const string TopAssistProvider = "top_assist_provider";
    public const string GoldenBoot = "golden_boot";
    public const string BestYoungPlayer = "best_young_player";
    public const string PlayerOfTheSeason = "player_of_the_season";
    public const string GoldenGlove = "golden_glove";
    public const string SurpriseTeam = "surprise_team";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        LeagueWinner,
        TopFour,
        Relegated,
        BestPlayer,
        TopGoalScorer,
        TopAssistProvider,
        GoldenBoot,
        BestYoungPlayer,
        PlayerOfTheSeason,
        GoldenGlove,
        SurpriseTeam
    };

    public static readonly HashSet<string> TeamTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        LeagueWinner,
        TopFour,
        Relegated,
        SurpriseTeam
    };

    public static readonly HashSet<string> PlayerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        BestPlayer,
        TopGoalScorer,
        TopAssistProvider,
        GoldenBoot,
        BestYoungPlayer,
        PlayerOfTheSeason,
        GoldenGlove
    };

    public static bool RequiresCountry(string type) => false;

    public static bool RequiresTeam(string type) => TeamTypes.Contains(type);

    public static bool RequiresPlayer(string type) => PlayerTypes.Contains(type);
}
