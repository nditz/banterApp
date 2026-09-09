namespace BanterApp.Api.Common;

public static class ErrorCategoryMapper
{
    public static string Map(string source, string? category)
    {
        if (IsBot(source, category))
        {
            return ErrorCodes.Forbidden;
        }

        if (string.Equals(source, "ssrf", StringComparison.OrdinalIgnoreCase) ||
            category?.Contains("ssrf", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ErrorCodes.Forbidden;
        }

        if (string.Equals(source, "background", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(source, "job", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorCodes.JobFailed;
        }

        if (string.Equals(source, "frontend", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorCodes.UnknownError;
        }

        if (category?.Contains("openai", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ErrorCodes.OpenAiApiError;
        }

        if (category?.Contains("youtube", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ErrorCodes.YouTubeApiError;
        }

        if (category?.Contains("rss", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ErrorCodes.RssFetchError;
        }

        if (string.Equals(source, "provider", StringComparison.OrdinalIgnoreCase) ||
            category?.Contains("provider", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ErrorCodes.ExternalApiError;
        }

        return ErrorCodes.InternalServerError;
    }

    public static bool IsBot(string source, string? category) =>
        string.Equals(source, "bot", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(category, "bot_blocked", StringComparison.OrdinalIgnoreCase);

    public static string MapProvider(string source, string? category)
    {
        if (IsBot(source, category))
        {
            return "bot";
        }

        if (category?.Contains("openai", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "openai";
        }

        if (category?.Contains("youtube", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "youtube";
        }

        if (category?.Contains("rss", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "rss";
        }

        return source switch
        {
            "api" or "backend" => "app",
            "background" or "job" => "job",
            "frontend" => "app",
            "ssrf" => "ssrf",
            "provider" => "provider",
            _ => "unknown"
        };
    }
}
