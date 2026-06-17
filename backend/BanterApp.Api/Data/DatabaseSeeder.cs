using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.SportsData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sports = scope.ServiceProvider.GetRequiredService<ISportsDataProvider>();
        var news = scope.ServiceProvider.GetRequiredService<INewsProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        var providerName = db.Database.ProviderName ?? "unknown";
        var isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        logger.LogInformation(
            "Database provider: {Provider} ({Mode})",
            providerName,
            isPostgres ? "Supabase/PostgreSQL (ConnectionStrings:DefaultConnection)" : "in-memory — copy appsettings.Development.json.example and set ConnectionStrings:DefaultConnection");

        if (isPostgres)
        {
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied.");
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!await db.Matches.AnyAsync(cancellationToken))
        {
            await SeedMatchesAsync(db, sports, cancellationToken);
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

        if (!await db.Leagues.AnyAsync(l => l.Kind == LeagueKind.Global, cancellationToken))
        {
            db.Leagues.Add(new League
            {
                Id = League.GlobalLeagueId,
                Name = "Global Banter League",
                InviteCode = "GLOBAL",
                Kind = LeagueKind.Global,
                MaxMembers = 1_000_000
            });
        }

        var useLiveSports = sports is not MockSportsDataProvider;

        if (!useLiveSports && !await db.Pundits.AnyAsync(cancellationToken))
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

        var matchCount = await db.Matches.CountAsync(cancellationToken);
        var newsCount = await db.NewsFeedItems.CountAsync(cancellationToken);
        logger.LogInformation(
            "Seed complete: {MatchCount} matches, {NewsCount} feed items in database.",
            matchCount,
            newsCount);
    }

    private static async Task SeedMatchesAsync(
        AppDbContext db,
        ISportsDataProvider sports,
        CancellationToken cancellationToken)
    {
        var all = await sports.GetAllFixturesAsync(cancellationToken);

        foreach (var dto in all)
        {
            db.Matches.Add(MatchMapper.FromDto(dto));
        }
    }

    private static async Task SeedMissingKnockoutMatchesAsync(
        AppDbContext db,
        ISportsDataProvider sports,
        CancellationToken cancellationToken)
    {
        var all = await sports.GetAllFixturesAsync(cancellationToken);
        var existingIds = await db.Matches.Select(m => m.Id).ToListAsync(cancellationToken);
        var existing = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dto in all)
        {
            if (!existing.Contains(dto.Id))
            {
                db.Matches.Add(MatchMapper.FromDto(dto));
            }
        }
    }
}
