using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.FootballReference;
using BanterApp.Api.Integrations.FootballReference.Jobs;
using BanterApp.Api.Integrations.Jobs;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Admin;

public sealed class FootballDataAdminService(
    AppDbContext db,
    FootballReferenceDataProviderFactory providerFactory,
    IOptions<FootballReferenceDataOptions> options,
    IRecurringJobManager recurringJobs)
{
    private static readonly string[] FootballJobIds =
    [
        FootballCountriesSyncJob.JobId,
        FootballPlayersSyncJob.JobId,
        FootballPlayerStatsSyncJob.JobId,
        FootballTopScorersSyncJob.JobId,
        FootballTopAssistsSyncJob.JobId,
        FootballReferenceFullSyncJob.JobId
    ];

    public async Task<FootballDataOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var provider = providerFactory.Resolve();
        var opt = options.Value;

        var countriesCount = await db.Countries.CountAsync(cancellationToken);
        var playersCount = await db.Players.CountAsync(cancellationToken);
        var statsCount = await db.PlayerStats.CountAsync(cancellationToken);
        var leaderboardCount = await db.LeaderboardEntries.CountAsync(cancellationToken);

        var syncRuns = await db.SyncRuns.AsNoTracking()
            .Where(r => FootballJobIds.Contains(r.JobName))
            .OrderByDescending(r => r.StartedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var lastSync = syncRuns.FirstOrDefault()?.StartedAt;
        var failedCount = syncRuns.Count(r => r.Status == "failed");

        var lastByJob = syncRuns
            .GroupBy(r => r.JobName)
            .ToDictionary(g => g.Key, g => g.First());

        return new FootballDataOverviewResponse(
            countriesCount,
            playersCount,
            statsCount,
            leaderboardCount,
            lastSync,
            failedCount,
            provider.ProviderName,
            opt.CompetitionCode,
            opt.Season,
            lastByJob.Select(kv => new FootballSyncJobStatus(
                kv.Key,
                kv.Value.Status,
                kv.Value.StartedAt,
                kv.Value.FinishedAt)).ToList());
    }

    public async Task<FootballCountriesListResponse> ListCountriesAsync(
        string? search,
        int? limit,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(limit ?? 50, 1, 200);
        var query = db.Countries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Code != null && c.Code.ToLower().Contains(term)));
        }

        var items = await query
            .OrderBy(c => c.Name)
            .Take(take)
            .Select(c => new FootballCountryAdminItem(
                c.Id,
                c.Name,
                c.Code,
                c.FlagUrl,
                c.IsActive,
                c.ExternalProvider,
                c.ExternalId,
                SanitizeMetadata(c.MetadataJson),
                c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new FootballCountriesListResponse(items);
    }

    public async Task<FootballPlayersListResponse> ListPlayersAsync(
        Guid? countryId,
        string? position,
        string? search,
        int? limit,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(limit ?? 50, 1, 200);
        var query = db.Players.AsNoTracking().AsQueryable();

        if (countryId is not null)
        {
            query = query.Where(p => p.CountryId == countryId);
        }

        if (!string.IsNullOrWhiteSpace(position))
        {
            query = query.Where(p => p.Position != null && p.Position.Contains(position.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.DisplayName.ToLower().Contains(term));
        }

        var items = await query
            .OrderBy(p => p.DisplayName)
            .Take(take)
            .Select(p => new FootballPlayerAdminItem(
                p.Id,
                p.DisplayName,
                p.Position,
                p.PhotoUrl,
                p.CountryId,
                p.Country != null ? p.Country.Name : null,
                p.IsActive,
                p.ExternalProvider,
                p.ExternalId,
                SanitizeMetadata(p.MetadataJson),
                p.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new FootballPlayersListResponse(items);
    }

    public async Task<FootballLeaderboardsListResponse> ListLeaderboardsAsync(
        string? type,
        CancellationToken cancellationToken)
    {
        var comp = options.Value.CompetitionCode;
        var season = options.Value.Season;
        var leaderboardType = string.IsNullOrWhiteSpace(type)
            ? LeaderboardTypes.TopScorers
            : type.Trim().ToLowerInvariant();

        var entries = await db.LeaderboardEntries
            .AsNoTracking()
            .Where(e => e.LeaderboardType == leaderboardType &&
                        e.Competition == comp &&
                        e.Season == season)
            .OrderBy(e => e.Rank ?? int.MaxValue)
            .Select(e => new FootballLeaderboardAdminItem(
                e.Id,
                e.Rank,
                e.Value,
                e.Player.DisplayName,
                e.Country != null ? e.Country.Name : null,
                e.SourceProvider,
                e.SourceUpdatedAt))
            .ToListAsync(cancellationToken);

        return new FootballLeaderboardsListResponse(leaderboardType, comp, season, entries);
    }

    public async Task<bool> SetCountryActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var country = await db.Countries.FindAsync([id], cancellationToken);
        if (country is null)
        {
            return false;
        }

        country.IsActive = isActive;
        country.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetPlayerActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var player = await db.Players.FindAsync([id], cancellationToken);
        if (player is null)
        {
            return false;
        }

        player.IsActive = isActive;
        player.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public void TriggerSync(string jobKey)
    {
        var def = JobRegistry.FindByKey(jobKey);
        if (def is null)
        {
            throw new InvalidOperationException($"Unknown job key: {jobKey}");
        }

        recurringJobs.Trigger(def.HangfireJobId);
    }

    private static string? SanitizeMetadata(string? metadataJson)
    {
        if (metadataJson is null) return null;
        var sanitized = SecretSanitizer.SanitizeJson(metadataJson);
        return sanitized.Length <= 500 ? sanitized : sanitized[..500];
    }
}

public sealed record FootballDataOverviewResponse(
    int CountriesCount,
    int PlayersCount,
    int StatsCount,
    int LeaderboardEntriesCount,
    DateTimeOffset? LastSyncAt,
    int FailedSyncCount,
    string CurrentProvider,
    string Competition,
    string Season,
    IReadOnlyList<FootballSyncJobStatus> RecentJobs);

public sealed record FootballSyncJobStatus(
    string JobName,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt);

public sealed record FootballCountryAdminItem(
    Guid Id,
    string Name,
    string? Code,
    string? FlagUrl,
    bool IsActive,
    string? ExternalProvider,
    string? ExternalId,
    string? MetadataPreview,
    DateTimeOffset UpdatedAt);

public sealed record FootballCountriesListResponse(IReadOnlyList<FootballCountryAdminItem> Countries);

public sealed record FootballPlayerAdminItem(
    Guid Id,
    string DisplayName,
    string? Position,
    string? PhotoUrl,
    Guid? CountryId,
    string? CountryName,
    bool IsActive,
    string? ExternalProvider,
    string? ExternalId,
    string? MetadataPreview,
    DateTimeOffset UpdatedAt);

public sealed record FootballPlayersListResponse(IReadOnlyList<FootballPlayerAdminItem> Players);

public sealed record FootballLeaderboardAdminItem(
    Guid Id,
    int? Rank,
    decimal Value,
    string PlayerName,
    string? CountryName,
    string? SourceProvider,
    DateTimeOffset? SourceUpdatedAt);

public sealed record FootballLeaderboardsListResponse(
    string LeaderboardType,
    string Competition,
    string Season,
    IReadOnlyList<FootballLeaderboardAdminItem> Entries);
