using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.SportsData;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sports = scope.ServiceProvider.GetRequiredService<ISportsDataProvider>();
        var news = scope.ServiceProvider.GetRequiredService<INewsProvider>();

        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (!await db.Matches.AnyAsync(cancellationToken))
        {
            var upcoming = await sports.GetUpcomingFixturesAsync(cancellationToken);
            var results = await sports.GetResultsAsync(cancellationToken);

            foreach (var dto in upcoming.Concat(results))
            {
                db.Matches.Add(new Match
                {
                    Id = dto.Id,
                    TeamA = dto.HomeTeam.Name,
                    TeamB = dto.AwayTeam.Name,
                    TeamACode = dto.HomeTeam.Code,
                    TeamBCode = dto.AwayTeam.Code,
                    KickoffTime = dto.KickoffUtc,
                    Stage = dto.Stage,
                    Group = dto.Group,
                    Venue = dto.Venue,
                    Status = dto.Status,
                    HomeScore = dto.HomeScore,
                    AwayScore = dto.AwayScore
                });
            }
        }

        if (!await db.NewsFeedItems.AnyAsync(cancellationToken))
        {
            var articles = await news.GetLatestArticlesAsync(20, cancellationToken);
            foreach (var article in articles)
            {
                db.NewsFeedItems.Add(new NewsFeedItem
                {
                    Id = article.Id,
                    Source = article.Source,
                    Title = article.Title,
                    Summary = article.Summary,
                    Url = article.Url,
                    Author = article.Author,
                    Category = article.Category,
                    PublishedAt = article.PublishedAt,
                    ViewCount = Random.Shared.Next(50, 5000)
                });
            }
        }

        if (!await db.Pundits.AnyAsync(cancellationToken))
        {
            var pundits = new[]
            {
                new Pundit { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Name = "Alex Morgan", Organization = "ESPN" },
                new Pundit { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Name = "Rio Ferdinand", Organization = "BBC Sport" },
                new Pundit { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Name = "Stephen A. Smith", Organization = "First Take" },
            };
            db.Pundits.AddRange(pundits);

            var finished = await db.Matches.Where(m => m.Status == "FT").Take(3).ToListAsync(cancellationToken);
            foreach (var match in finished)
            {
                db.PunditPredictions.Add(new PunditPrediction
                {
                    Id = Guid.NewGuid(),
                    PunditId = pundits[0].Id,
                    MatchId = match.Id,
                    Prediction = "Home Win",
                    PublishedAt = match.KickoffTime.AddDays(-1)
                });
                db.PunditPredictions.Add(new PunditPrediction
                {
                    Id = Guid.NewGuid(),
                    PunditId = pundits[1].Id,
                    MatchId = match.Id,
                    Prediction = "Draw",
                    PublishedAt = match.KickoffTime.AddDays(-1)
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
