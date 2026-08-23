namespace BanterApp.Api.Integrations.News;

public sealed class MockNewsProvider : INewsProvider
{
    private static readonly IReadOnlyList<NewsArticleDto> Articles =
    [
        new(
            "news-001",
            "Arsenal edge Liverpool as Havertz strike decides the North London-Merseyside clash",
            "A late winner at Anfield keeps the title race wide open after a tense Premier League night.",
            "ESPN",
            "https://www.espn.com/soccer/story/_/id/example-arsenal-liverpool",
            "Mark Ogden",
            DateTimeOffset.UtcNow.AddHours(-8),
            null,
            "Match Report"),
        new(
            "news-002",
            "Manchester City put four past Chelsea to send a statement",
            "Pep Guardiola's side looked ruthless in a statement win that puts pressure on the chasing pack.",
            "The Guardian",
            "https://www.theguardian.com/football/example-man-city-chelsea",
            "Jonathan Wilson",
            DateTimeOffset.UtcNow.AddHours(-14),
            null,
            "Match Report"),
        new(
            "news-003",
            "Arsenal vs Manchester City: title-race preview",
            "Two of the Premier League's heavyweights meet with the table still tight at the top.",
            "BBC Sport",
            "https://www.bbc.com/sport/football/example-arsenal-city-preview",
            "Phil McNulty",
            DateTimeOffset.UtcNow.AddHours(-2),
            null,
            "Preview"),
        new(
            "news-004",
            "Newcastle grind out a win as Isak stays clinical",
            "Eddie Howe's side took the points in a tight home fixture that keeps European hopes alive.",
            "Sky Sports",
            "https://www.skysports.com/football/news/example-newcastle-villa",
            "Gerard Brand",
            DateTimeOffset.UtcNow.AddDays(-1).AddHours(-3),
            null,
            "Match Report"),
        new(
            "news-005",
            "Premier League 2026/27: how the title race and relegation fight work",
            "A beginner-friendly guide to 38 matchweeks, 20 clubs, and why every pick still matters in May.",
            "The Athletic",
            "https://www.nytimes.com/athletic/example-premier-league-guide",
            "Mark Ogden",
            DateTimeOffset.UtcNow.AddDays(-2),
            null,
            "Explainer"),
    ];

    public Task<IReadOnlyList<NewsArticleDto>> GetLatestArticlesAsync(
        int count = 10,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NewsArticleDto>>(Articles.Take(count).ToList());
}
