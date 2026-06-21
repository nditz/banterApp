namespace BanterApp.Api.Integrations.FootballBanter;

public static class FootballBanterGifMoodResolver
{
    public static string Resolve(IReadOnlyList<string> gifSuggestions, string? categoryFallback = "news")
    {
        if (gifSuggestions.Count > 0)
        {
            var text = string.Join(' ', gifSuggestions).ToLowerInvariant();
            if (text.Contains("angry", StringComparison.Ordinal) || text.Contains("keane", StringComparison.Ordinal))
            {
                return "debate";
            }

            if (text.Contains("laugh", StringComparison.Ordinal) || text.Contains("smil", StringComparison.Ordinal))
            {
                return "celebrate";
            }

            if (text.Contains("shock", StringComparison.Ordinal) || text.Contains("react", StringComparison.Ordinal))
            {
                return "shock";
            }

            if (text.Contains("facepalm", StringComparison.Ordinal))
            {
                return "facepalm";
            }
        }

        return categoryFallback?.Trim().ToLowerInvariant() switch
        {
            "pundit_quote" => "pundit",
            "match_live" => "hype",
            "match_result" => "celebrate",
            "match_fixture" => "debate",
            "youtube" => "hype",
            "rss" => "news",
            _ => "news"
        };
    }
}
