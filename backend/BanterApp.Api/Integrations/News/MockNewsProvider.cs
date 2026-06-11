namespace BanterApp.Api.Integrations.News;

public sealed class MockNewsProvider : INewsProvider
{
    private static readonly IReadOnlyList<NewsArticleDto> Articles =
    [
        new(
            "news-001",
            "USA edge Canada in World Cup 2026 opener as Pulisic strikes late",
            "Christian Pulisic's 78th-minute goal sealed a 2-1 win for the hosts in a tense Group A clash at MetLife Stadium.",
            "ESPN",
            "https://www.espn.com/soccer/story/_/id/example-usa-canada-wc26",
            "Jeff Carlisle",
            DateTimeOffset.UtcNow.AddHours(-8),
            null,
            "Match Report"),
        new(
            "news-002",
            "Mexico send statement with 3-0 win over Jamaica at Azteca",
            "El Tri dominated from the first whistle, with Lozano and Jimenez on the scoresheet in a roaring home atmosphere.",
            "The Guardian",
            "https://www.theguardian.com/football/example-mexico-jamaica-wc26",
            "Jonathan Wilson",
            DateTimeOffset.UtcNow.AddHours(-14),
            null,
            "Match Report"),
        new(
            "news-003",
            "England vs France preview: Kane and Mbappe set for Group B blockbuster",
            "Two European heavyweights collide in Dallas with both sides eyeing early control of the so-called Group of Death.",
            "BBC Sport",
            "https://www.bbc.com/sport/football/example-england-france-preview",
            "Phil McNulty",
            DateTimeOffset.UtcNow.AddHours(-2),
            null,
            "Preview"),
        new(
            "news-004",
            "Brazil held by Serbia in Los Angeles as both teams share the spoils",
            "A Vinicius Jr equalizer rescued Brazil after Mitrovic's early header had given Serbia a surprise lead at SoFi Stadium.",
            "Sky Sports",
            "https://www.skysports.com/football/news/example-brazil-serbia-draw",
            "Gerard Brand",
            DateTimeOffset.UtcNow.AddDays(-1).AddHours(-3),
            null,
            "Match Report"),
        new(
            "news-005",
            "World Cup 2026 format explained: 48 teams, 12 groups, and the road to the final",
            "A beginner-friendly guide to the expanded tournament across the USA, Canada, and Mexico.",
            "The Athletic",
            "https://www.nytimes.com/athletic/example-wc26-format-guide",
            "Mark Ogden",
            DateTimeOffset.UtcNow.AddDays(-2),
            null,
            "Explainer"),
    ];

    public Task<IReadOnlyList<NewsArticleDto>> GetLatestArticlesAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(count, 1, Articles.Count);
        return Task.FromResult<IReadOnlyList<NewsArticleDto>>(Articles.Take(take).ToList());
    }
}
