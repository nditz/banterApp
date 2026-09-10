namespace BanterApp.Api.Data;

/// <summary>
/// The live product's active competition. Prompts and YouTube search read this
/// so generated copy stays on Premier League 2026/27.
/// Swap <see cref="PremierLeagueCatalog"/> when the product adds another league.
/// </summary>
public static class CompetitionFocus
{
    public static string Name => PremierLeagueCatalog.Name;

    public static string Season => PremierLeagueCatalog.CurrentSeasonName;

    public static string DisplayName => $"{Name} {Season}";

    public static string PromptDirective =>
        $"The product currently covers {DisplayName} only. " +
        "Search, extract, and write about that competition. " +
        "Ignore World Cup, FIFA tournaments, international friendlies, and other leagues " +
        "unless they directly affect a Premier League club this season. " +
        "Do not invent World Cup predictions, national-team frames, or Brazil/England tournament tropes.";

    public static string ApplyToSystemPrompt(string systemPrompt)
    {
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            return PromptDirective;
        }

        if (systemPrompt.Contains(DisplayName, StringComparison.OrdinalIgnoreCase) &&
            systemPrompt.Contains("Ignore World Cup", StringComparison.OrdinalIgnoreCase))
        {
            return systemPrompt;
        }

        return PromptDirective + "\n\n" + systemPrompt.Trim();
    }

    public static IReadOnlyList<string> YouTubeSearchQueries { get; } =
    [
        "Premier League 2026/27 predictions pundits",
        "Premier League weekend preview predictions",
        "Sky Sports Premier League Super Sunday predictions",
        "Monday Night Football Premier League predictions",
        "The Rest Is Football Premier League",
        "Stick to Football Premier League predictions",
        "Gary Neville Premier League prediction",
        "BBC Match of the Day Premier League analysis"
    ];
}
