using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Sync;

/// <summary>
/// Removes seeded demo/mock rows so live sync jobs can repopulate from external providers.
/// </summary>
public sealed class LiveDataResetService(AppDbContext db, ILogger<LiveDataResetService> logger)
{
    public async Task<LiveDataResetResult> ResetDemoDataAsync(CancellationToken cancellationToken = default)
    {
        var demoMatchIds = await db.Matches
            .Where(m => m.Id.StartsWith("pl26-") || m.Id.StartsWith("wc26-") || m.Id.StartsWith("of26-"))
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

        var matchesRemoved = demoMatchIds.Count > 0
            ? await db.Matches.Where(m => demoMatchIds.Contains(m.Id)).ExecuteDeleteAsync(cancellationToken)
            : 0;

        var newsRemoved = await db.NewsFeedItems.ExecuteDeleteAsync(cancellationToken);

        var personaIds = await db.Pundits
            .Where(p => p.Kind == PunditKind.Persona)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var punditPredictionsRemoved = personaIds.Count > 0
            ? await db.PunditPredictions
                .Where(p => personaIds.Contains(p.PunditId))
                .ExecuteDeleteAsync(cancellationToken)
            : 0;

        var punditsRemoved = personaIds.Count > 0
            ? await db.Pundits
                .Where(p => personaIds.Contains(p.Id))
                .ExecuteDeleteAsync(cancellationToken)
            : 0;

        var aiContentRemoved = await db.GeneratedContents.ExecuteDeleteAsync(cancellationToken);

        logger.LogWarning(
            "Demo data reset: {Matches} matches, {News} feed items, {Pundits} pundits, {Predictions} predictions removed.",
            matchesRemoved,
            newsRemoved,
            punditsRemoved,
            predictionsRemoved);

        return new LiveDataResetResult(
            matchesRemoved,
            newsRemoved,
            punditsRemoved,
            punditPredictionsRemoved,
            predictionsRemoved,
            aiContentRemoved);
    }
}

public sealed record LiveDataResetResult(
    int MatchesRemoved,
    int NewsFeedItemsRemoved,
    int PunditsRemoved,
    int PunditPredictionsRemoved,
    int PredictionsRemoved,
    int GeneratedContentRemoved);
