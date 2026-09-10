using BanterApp.Api.Data;

namespace BanterApp.Api.Integrations.Media;

public sealed class YouTubeOptions
{
    public const string SectionName = "YouTube";

    public string BaseUrl { get; set; } = "https://www.googleapis.com/youtube/v3";

    public string? ApiKey { get; set; }

    public string[] DefaultSearchTerms { get; set; } = [.. CompetitionFocus.YouTubeSearchQueries];
}
