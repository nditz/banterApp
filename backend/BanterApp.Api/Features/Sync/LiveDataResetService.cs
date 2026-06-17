using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Sync;

/// <summary>
/// Removes seeded demo/mock rows so live sync jobs can repopulate from external providers.
/// </summary>
public sealed class LiveDataResetService(AppDbContext db, ILogger<LiveDataResetService> logger)
{
    private static readonly Guid[] SeedPunditIds =
    [
        Guid.Parse("11111111-1111-1111-1111-111111111101"),
        Guid.Parse("11111111-1111-1111-1111-111111111102"),
        Guid.Parse("11111111-1111-1111-1111-111111111103"),
    ];

    public async Task<LiveDataResetResult> ResetDemoDataAsync(CancellationToken cancellationToken = default)
    {
        var demoMatchIds = await db.Matches
            .Where(m => m.Id.StartsWith("wc26-"))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var predictionsRemoved = 0;
        if (demoMatchIds.Count > 0)
        {
            predictionsRemoved = await db.Predictions
                .Where(p => demoMatchIds.Contains(p.MatchId))
                .ExecuteDeleteAsync(cancellationToken);

            await db.PunditPredictions
                .Where(p => demoMatchIds.Contains(p.MatchId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var bracketPicksRemoved = await db.BracketPicks.ExecuteDeleteAsync(cancellationToken);

        var matchesRemoved = demoMatchIds.Count > 0
            ? await db.Matches.Where(m => demoMatchIds.Contains(m.Id)).ExecuteDeleteAsync(cancellationToken)
            : 0;

        var newsRemoved = await db.NewsFeedItems.ExecuteDeleteAsync(cancellationToken);

        var punditPredictionsRemoved = await db.PunditPredictions
            .Where(p => SeedPunditIds.Contains(p.PunditId))
            .ExecuteDeleteAsync(cancellationToken);

        var punditsRemoved = await db.Pundits
            .Where(p => SeedPunditIds.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken);

        var aiContentRemoved = await db.GeneratedContents.ExecuteDeleteAsync(cancellationToken);

        logger.LogWarning(
            "Demo data reset: {Matches} matches, {News} feed items, {Pundits} pundits, {Predictions} predictions, {BracketPicks} bracket picks removed.",
            matchesRemoved,
            newsRemoved,
            punditsRemoved,
            predictionsRemoved,
            bracketPicksRemoved);

        return new LiveDataResetResult(
            matchesRemoved,
            newsRemoved,
            punditsRemoved,
            punditPredictionsRemoved,
            predictionsRemoved,
            aiContentRemoved,
            bracketPicksRemoved);
    }
}

public sealed record LiveDataResetResult(
    int MatchesRemoved,
    int NewsFeedItemsRemoved,
    int PunditsRemoved,
    int PunditPredictionsRemoved,
    int PredictionsRemoved,
    int GeneratedContentRemoved,
    int BracketPicksRemoved);
