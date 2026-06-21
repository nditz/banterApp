namespace BanterApp.Api.Integrations.FootballBanter;

public interface IFootballBanterConfigProvider
{
    FootballBanterConfig Config { get; }

    string SystemPrompt { get; }

    IReadOnlyDictionary<string, string> RssFeedSourceNames { get; }

    bool IsValid { get; }

    IReadOnlyList<string> LoadErrors { get; }

    IReadOnlyList<string> LoadWarnings { get; }
}

public sealed class FootballBanterConfigProvider : IFootballBanterConfigProvider
{
    public FootballBanterConfig Config { get; }

    public string SystemPrompt { get; }

    public IReadOnlyDictionary<string, string> RssFeedSourceNames { get; }

    public bool IsValid { get; }

    public IReadOnlyList<string> LoadErrors { get; }

    public IReadOnlyList<string> LoadWarnings { get; }

    private FootballBanterConfigProvider(
        FootballBanterConfig config,
        string systemPrompt,
        IReadOnlyDictionary<string, string> rssFeedSourceNames,
        bool isValid,
        IReadOnlyList<string> loadErrors,
        IReadOnlyList<string> loadWarnings)
    {
        Config = config;
        SystemPrompt = systemPrompt;
        RssFeedSourceNames = rssFeedSourceNames;
        IsValid = isValid;
        LoadErrors = loadErrors;
        LoadWarnings = loadWarnings;
    }

    public static FootballBanterConfigProvider Create(string contentRootPath, ILogger? logger = null)
    {
        var loadResult = FootballBanterConfigLoader.LoadFromContentRoot(contentRootPath);
        var systemPrompt = FootballBanterConfigLoader.LoadSystemPromptFromContentRoot(contentRootPath);
        var rssMap = BuildRssFeedSourceMap(loadResult.Config);

        foreach (var warning in loadResult.Warnings)
        {
            logger?.LogWarning("Football Banter config: {Warning}", warning);
        }

        foreach (var error in loadResult.Errors)
        {
            logger?.LogError("Football Banter config: {Error}", error);
        }

        if (!loadResult.IsValid)
        {
            logger?.LogWarning(
                "Football Banter config is invalid; using safe defaults for missing pipeline values.");
        }

        return new FootballBanterConfigProvider(
            loadResult.Config,
            systemPrompt,
            rssMap,
            loadResult.IsValid,
            loadResult.Errors,
            loadResult.Warnings);
    }

    public static IReadOnlyDictionary<string, string> BuildRssFeedSourceMap(FootballBanterConfig config)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var feed in config.Sources.Rss.Feeds)
        {
            if (string.IsNullOrWhiteSpace(feed.Url))
            {
                continue;
            }

            map[feed.Url.Trim()] = string.IsNullOrWhiteSpace(feed.SourceName)
                ? feed.Name
                : feed.SourceName.Trim();
        }

        return map;
    }
}
