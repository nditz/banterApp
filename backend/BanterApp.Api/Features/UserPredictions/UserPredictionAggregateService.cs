using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.UserPredictions;

public sealed class UserPredictionAggregateService(AppDbContext db)
{
    public async Task<PredictionAggregateResponse> GetAggregatesAsync(
        string? predictionType,
        string? competition,
        string? season,
        CancellationToken cancellationToken = default)
    {
        var query = db.UserPredictions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(predictionType))
        {
            query = query.Where(p => p.PredictionType == predictionType);
        }

        if (!string.IsNullOrWhiteSpace(competition))
        {
            query = query.Where(p => p.Competition == competition);
        }

        if (!string.IsNullOrWhiteSpace(season))
        {
            query = query.Where(p => p.Season == season);
        }

        var predictions = await query.ToListAsync(cancellationToken);

        if (predictions.Count == 0)
        {
            return new PredictionAggregateResponse(
                predictionType ?? "all",
                []);
        }

        var typeGroups = predictions.GroupBy(p => p.PredictionType);
        var results = new List<PredictionTypeAggregate>();

        foreach (var group in typeGroups)
        {
            var total = group.Count();
            List<PredictionAggregateEntry> entries;

            if (UserPredictionTypes.RequiresPlayer(group.Key))
            {
                var playerIds = group.Where(p => p.PlayerId != null).Select(p => p.PlayerId!.Value).Distinct().ToList();
                var players = await db.Players.AsNoTracking()
                    .Include(p => p.Country)
                    .Where(p => playerIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

                entries = group
                    .Where(p => p.PlayerId != null)
                    .GroupBy(p => p.PlayerId!.Value)
                    .Select(g =>
                    {
                        players.TryGetValue(g.Key, out var player);
                        var count = g.Count();
                        return new PredictionAggregateEntry(
                            g.Key,
                            null,
                            player?.DisplayName ?? "Unknown",
                            player?.Country?.Name,
                            count,
                            total > 0 ? Math.Round(count * 100.0 / total, 1) : 0);
                    })
                    .OrderByDescending(e => e.PredictionCount)
                    .Take(20)
                    .ToList();
            }
            else
            {
                var countryIds = group.Where(p => p.CountryId != null).Select(p => p.CountryId!.Value).Distinct().ToList();
                var countries = await db.Countries.AsNoTracking()
                    .Where(c => countryIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, cancellationToken);

                entries = group
                    .Where(p => p.CountryId != null)
                    .GroupBy(p => p.CountryId!.Value)
                    .Select(g =>
                    {
                        countries.TryGetValue(g.Key, out var country);
                        var count = g.Count();
                        return new PredictionAggregateEntry(
                            null,
                            g.Key,
                            country?.Name ?? "Unknown",
                            country?.Name,
                            count,
                            total > 0 ? Math.Round(count * 100.0 / total, 1) : 0);
                    })
                    .OrderByDescending(e => e.PredictionCount)
                    .Take(20)
                    .ToList();
            }

            results.Add(new PredictionTypeAggregate(group.Key, entries));
        }

        if (!string.IsNullOrWhiteSpace(predictionType))
        {
            var match = results.FirstOrDefault(r =>
                string.Equals(r.PredictionType, predictionType, StringComparison.OrdinalIgnoreCase));
            return new PredictionAggregateResponse(
                predictionType,
                match?.Entries ?? []);
        }

        return new PredictionAggregateResponse("all", results.SelectMany(r => r.Entries).ToList());
    }
}

public sealed record PredictionAggregateEntry(
    Guid? PlayerId,
    Guid? CountryId,
    string Name,
    string? Country,
    int PredictionCount,
    double Percentage);

public sealed record PredictionTypeAggregate(
    string PredictionType,
    IReadOnlyList<PredictionAggregateEntry> Entries);

public sealed record PredictionAggregateResponse(
    string PredictionType,
    IReadOnlyList<PredictionAggregateEntry> Entries);
