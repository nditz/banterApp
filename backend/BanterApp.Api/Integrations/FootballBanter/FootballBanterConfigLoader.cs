namespace BanterApp.Api.Integrations.FootballBanter;

public sealed class FootballBanterConfigLoadResult
{
    public FootballBanterConfig Config { get; init; } = new();

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public bool IsValid => Errors.Count == 0;
}

public static class FootballBanterConfigLoader
{
    public const string ConfigRelativePath = "config/football-banter.config.json";
    public const string PromptRelativePath = "prompts/football-banter-system-prompt.md";

    public static FootballBanterConfigLoadResult LoadFromContentRoot(string contentRootPath)
    {
        var configPath = Path.Combine(contentRootPath, ConfigRelativePath);
        if (!File.Exists(configPath))
        {
            return new FootballBanterConfigLoadResult
            {
                Errors = [$"Football Banter config not found at '{configPath}'."]
            };
        }

        var json = File.ReadAllText(configPath);
        return LoadFromJson(json);
    }

    public static FootballBanterConfigLoadResult LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FootballBanterConfigLoadResult
            {
                Errors = ["Football Banter config JSON is empty."]
            };
        }

        try
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<FootballBanterConfig>(
                json,
                FootballBanterJson.Options);

            if (config is null)
            {
                return new FootballBanterConfigLoadResult
                {
                    Errors = ["Football Banter config deserialized to null."]
                };
            }

            return FootballBanterConfigValidator.ValidateAndApplyDefaults(config);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return new FootballBanterConfigLoadResult
            {
                Errors = [$"Football Banter config JSON is invalid: {ex.Message}"]
            };
        }
    }

    public static string LoadSystemPromptFromContentRoot(string contentRootPath)
    {
        var promptPath = Path.Combine(contentRootPath, PromptRelativePath);
        if (!File.Exists(promptPath))
        {
            return FootballBanterDefaults.EmbeddedSystemPromptFallback;
        }

        var prompt = File.ReadAllText(promptPath).Trim();
        return string.IsNullOrWhiteSpace(prompt)
            ? FootballBanterDefaults.EmbeddedSystemPromptFallback
            : prompt;
    }
}
