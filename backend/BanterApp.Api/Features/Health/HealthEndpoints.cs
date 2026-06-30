using BanterApp.Api.Data;
using BanterApp.Api.Integrations.SportsData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .AllowAnonymous()
            .ExcludeFromDescription();

        app.MapGet("/api/health", async (
            AppDbContext db,
            IOptions<SportsDataOptions> sportsOptions,
            CancellationToken ct) =>
        {
            var sports = sportsOptions.Value;
            var hasSportsKey = !string.IsNullOrWhiteSpace(sports.ApiKey);
            var sportsMode = sports.Provider switch
            {
                "apifootball" when hasSportsKey => "apifootball-live",
                "apifootball" => "apifootball-mock-fallback",
                _ => "mock"
            };

            try
            {
                var canConnect = await db.Database.CanConnectAsync(ct);
                if (!canConnect)
                {
                    return Results.Json(
                        new { status = "degraded", database = new { connected = false, provider = "unknown" } },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                var isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
                var matchCount = await db.Matches.CountAsync(ct);
                var newsCount = await db.NewsFeedItems.CountAsync(ct);

                return Results.Ok(new
                {
                    status = "ok",
                    database = new
                    {
                        connected = true,
                        provider = isPostgres ? "postgresql" : "inmemory",
                        matchCount,
                        newsCount
                    },
                    sportsData = new
                    {
                        provider = sports.Provider,
                        mode = sportsMode,
                        syncIntervalMinutes = sports.SyncIntervalMinutes
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new { status = "error", message = ex.Message },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .AllowAnonymous()
        .ExcludeFromDescription();
    }
}
