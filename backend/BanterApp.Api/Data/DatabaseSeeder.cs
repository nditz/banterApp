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
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations applied.");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning", StringComparison.Ordinal))
            {
                logger.LogCritical(
                    ex,
                    "The EF model has changes that are not captured in a migration. " +
                    "Stop the API, run: dotnet ef migrations add <Name> --project backend/BanterApp.Api " +
                    "then dotnet ef database update --project backend/BanterApp.Api, and rebuild before restarting.");
                throw;
            }
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

        await PurgeFictionalPersonasAsync(db, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        var matchCount = await db.Matches.CountAsync(cancellationToken);
        var newsCount = await db.NewsFeedItems.CountAsync(cancellationToken);
        logger.LogInformation(
            "Seed complete: {MatchCount} matches, {NewsCount} feed items in database.",
            matchCount,
            newsCount);
    }

    /// <summary>Removes fictional parody desk rows so only real sourced pundits remain.</summary>
    private static async Task PurgeFictionalPersonasAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var personaIds = await db.Pundits
            .Where(p => p.Kind == PunditKind.Persona)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (personaIds.Count == 0)
        {
            return;
        }

        await db.PunditPredictions
            .Where(p => personaIds.Contains(p.PunditId))
            .ExecuteDeleteAsync(cancellationToken);

        await db.PunditOpinions
            .Where(o => personaIds.Contains(o.PunditId))
            .ExecuteDeleteAsync(cancellationToken);

        await db.Pundits
            .Where(p => personaIds.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken);
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
