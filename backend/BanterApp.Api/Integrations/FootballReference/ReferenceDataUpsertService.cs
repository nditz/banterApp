using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.FootballReference.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.FootballReference;

public sealed class ReferenceDataUpsertService(AppDbContext db)
{
    public async Task<(int Created, int Updated)> UpsertCountriesAsync(
        IReadOnlyList<CountryDto> items,
        string provider,
        CancellationToken cancellationToken = default)
    {
        var created = 0;
        var updated = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var dto in items)
        {
            var normalizedCode = dto.Code?.ToUpperInvariant();
            var existing = await db.Countries.FirstOrDefaultAsync(
                c => c.ExternalProvider == provider && c.ExternalId == dto.ExternalId,
                cancellationToken);

            if (existing is null && !string.IsNullOrWhiteSpace(normalizedCode))
            {
                existing = await db.Countries.FirstOrDefaultAsync(
                    c => c.Code == normalizedCode,
                    cancellationToken);
            }

            if (existing is null)
            {
                db.Countries.Add(new Country
                {
                    Id = Guid.NewGuid(),
                    ExternalId = dto.ExternalId,
                    ExternalProvider = provider,
                    Name = dto.Name,
                    Code = normalizedCode,
                    FlagUrl = dto.FlagUrl,
                    Continent = dto.Continent,
                    FifaRanking = dto.FifaRanking,
                    IsActive = true,
                    MetadataJson = StringLimits.Truncate(dto.MetadataJson, 8000),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                created++;
            }
            else
            {
                existing.Name = dto.Name;
                existing.Code = normalizedCode ?? existing.Code;
                existing.ExternalId ??= dto.ExternalId;
                existing.ExternalProvider ??= provider;
                existing.FlagUrl = dto.FlagUrl ?? existing.FlagUrl;
                existing.Continent = dto.Continent ?? existing.Continent;
                existing.FifaRanking = dto.FifaRanking ?? existing.FifaRanking;
                existing.MetadataJson = StringLimits.Truncate(dto.MetadataJson, 8000) ?? existing.MetadataJson;
                existing.UpdatedAt = now;
                updated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    public async Task<(int Created, int Updated)> UpsertPlayersAsync(
        IReadOnlyList<PlayerDto> items,
        string provider,
        CancellationToken cancellationToken = default)
    {
        var created = 0;
        var updated = 0;
        var now = DateTimeOffset.UtcNow;

        var countryMap = await BuildCountryExternalMapAsync(provider, cancellationToken);

        foreach (var dto in items)
        {
            Guid? countryId = null;
            if (!string.IsNullOrWhiteSpace(dto.CountryExternalId) &&
                countryMap.TryGetValue(dto.CountryExternalId, out var mappedCountryId))
            {
                countryId = mappedCountryId;
            }

            var existing = await db.Players.FirstOrDefaultAsync(
                p => p.ExternalProvider == provider && p.ExternalId == dto.ExternalId,
                cancellationToken);

            if (existing is null)
            {
                db.Players.Add(new Player
                {
                    Id = Guid.NewGuid(),
                    ExternalId = dto.ExternalId,
                    ExternalProvider = provider,
                    CountryId = countryId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    DisplayName = dto.DisplayName,
                    KnownName = dto.KnownName,
                    DateOfBirth = dto.DateOfBirth,
                    Age = dto.Age,
                    Position = dto.Position,
                    PhotoUrl = dto.PhotoUrl,
                    ClubName = dto.ClubName,
                    NationalTeamName = dto.NationalTeamName,
                    IsActive = true,
                    MetadataJson = StringLimits.Truncate(dto.MetadataJson, 8000),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                created++;
            }
            else
            {
                existing.CountryId = countryId ?? existing.CountryId;
                existing.FirstName = dto.FirstName ?? existing.FirstName;
                existing.LastName = dto.LastName ?? existing.LastName;
                existing.DisplayName = dto.DisplayName;
                existing.KnownName = dto.KnownName ?? existing.KnownName;
                existing.DateOfBirth = dto.DateOfBirth ?? existing.DateOfBirth;
                existing.Age = dto.Age ?? existing.Age;
                existing.Position = dto.Position ?? existing.Position;
                existing.PhotoUrl = dto.PhotoUrl ?? existing.PhotoUrl;
                existing.ClubName = dto.ClubName ?? existing.ClubName;
                existing.NationalTeamName = dto.NationalTeamName ?? existing.NationalTeamName;
                existing.MetadataJson = StringLimits.Truncate(dto.MetadataJson, 8000) ?? existing.MetadataJson;
                existing.UpdatedAt = now;
                updated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    public async Task<(int Created, int Updated)> UpsertPlayerStatsAsync(
        IReadOnlyList<PlayerStatsDto> items,
        string provider,
        CancellationToken cancellationToken = default)
    {
        var created = 0;
        var updated = 0;
        var now = DateTimeOffset.UtcNow;

        var playerMap = await BuildPlayerExternalMapAsync(provider, cancellationToken);
        var countryMap = await BuildCountryExternalMapAsync(provider, cancellationToken);

        foreach (var dto in items)
        {
            if (!playerMap.TryGetValue(dto.PlayerExternalId, out var playerId))
            {
                continue;
            }

            Guid? countryId = null;
            if (!string.IsNullOrWhiteSpace(dto.CountryExternalId) &&
                countryMap.TryGetValue(dto.CountryExternalId, out var mappedCountryId))
            {
                countryId = mappedCountryId;
            }

            var competition = dto.Competition ?? string.Empty;
            var season = dto.Season ?? string.Empty;

            var existing = await db.PlayerStats.FirstOrDefaultAsync(
                s => s.PlayerId == playerId &&
                     s.CountryId == countryId &&
                     s.Competition == competition &&
                     s.Season == season &&
                     s.SourceProvider == provider,
                cancellationToken);

            if (existing is null)
            {
                db.PlayerStats.Add(new PlayerStat
                {
                    Id = Guid.NewGuid(),
                    PlayerId = playerId,
                    CountryId = countryId,
                    Competition = competition,
                    Season = season,
                    MatchesPlayed = dto.MatchesPlayed,
                    Goals = dto.Goals,
                    Assists = dto.Assists,
                    YellowCards = dto.YellowCards,
                    RedCards = dto.RedCards,
                    MinutesPlayed = dto.MinutesPlayed,
                    Rating = dto.Rating,
                    SourceProvider = provider,
                    SourceUpdatedAt = now,
                    MetadataJson = StringLimits.Truncate(dto.MetadataJson, 8000),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                created++;
            }
            else
            {
                existing.MatchesPlayed = dto.MatchesPlayed;
                existing.Goals = dto.Goals;
                existing.Assists = dto.Assists;
                existing.YellowCards = dto.YellowCards;
                existing.RedCards = dto.RedCards;
                existing.MinutesPlayed = dto.MinutesPlayed;
                existing.Rating = dto.Rating ?? existing.Rating;
                existing.SourceUpdatedAt = now;
                existing.MetadataJson = StringLimits.Truncate(dto.MetadataJson, 8000) ?? existing.MetadataJson;
                existing.UpdatedAt = now;
                updated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    public async Task<(int Created, int Updated)> UpsertLeaderboardAsync(
        IReadOnlyList<LeaderboardEntryDto> items,
        string leaderboardType,
        string provider,
        string? competition,
        string? season,
        CancellationToken cancellationToken = default)
    {
        var created = 0;
        var updated = 0;
        var now = DateTimeOffset.UtcNow;
        var comp = competition ?? string.Empty;
        var seas = season ?? string.Empty;

        var playerMap = await BuildPlayerExternalMapAsync(provider, cancellationToken);
        var countryMap = await BuildCountryExternalMapAsync(provider, cancellationToken);

        var existingEntries = await db.LeaderboardEntries
            .Where(e => e.LeaderboardType == leaderboardType &&
                        e.Competition == comp &&
                        e.Season == seas &&
                        e.SourceProvider == provider)
            .ToListAsync(cancellationToken);

        var seenPlayerIds = new HashSet<Guid>();

        foreach (var dto in items)
        {
            if (!playerMap.TryGetValue(dto.PlayerExternalId, out var playerId))
            {
                continue;
            }

            seenPlayerIds.Add(playerId);
            Guid? countryId = null;
            if (!string.IsNullOrWhiteSpace(dto.CountryExternalId) &&
                countryMap.TryGetValue(dto.CountryExternalId, out var mappedCountryId))
            {
                countryId = mappedCountryId;
            }

            var existing = existingEntries.FirstOrDefault(e => e.PlayerId == playerId);
            if (existing is null)
            {
                db.LeaderboardEntries.Add(new LeaderboardEntry
                {
                    Id = Guid.NewGuid(),
                    LeaderboardType = leaderboardType,
                    PlayerId = playerId,
                    CountryId = countryId,
                    Rank = dto.Rank,
                    Value = dto.Value,
                    Competition = comp,
                    Season = seas,
                    SourceProvider = provider,
                    SourceUpdatedAt = now,
                    MetadataJson = StringLimits.Truncate(dto.MetadataJson, 8000),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                created++;
            }
            else
            {
                existing.CountryId = countryId ?? existing.CountryId;
                existing.Rank = dto.Rank ?? existing.Rank;
                existing.Value = dto.Value;
                existing.SourceUpdatedAt = now;
                existing.MetadataJson = StringLimits.Truncate(dto.MetadataJson, 8000) ?? existing.MetadataJson;
                existing.UpdatedAt = now;
                updated++;
            }
        }

        foreach (var stale in existingEntries.Where(e => !seenPlayerIds.Contains(e.PlayerId)))
        {
            db.LeaderboardEntries.Remove(stale);
        }

        await db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    private async Task<Dictionary<string, Guid>> BuildCountryExternalMapAsync(
        string provider,
        CancellationToken cancellationToken)
    {
        return await db.Countries
            .AsNoTracking()
            .Where(c => c.ExternalProvider == provider && c.ExternalId != null)
            .ToDictionaryAsync(c => c.ExternalId!, c => c.Id, cancellationToken);
    }

    private async Task<Dictionary<string, Guid>> BuildPlayerExternalMapAsync(
        string provider,
        CancellationToken cancellationToken)
    {
        return await db.Players
            .AsNoTracking()
            .Where(p => p.ExternalProvider == provider && p.ExternalId != null)
            .ToDictionaryAsync(p => p.ExternalId!, p => p.Id, cancellationToken);
    }
}
