using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Hangfire recurring job that polls the configured sports data provider
/// and upserts fixtures/scores into the database.
/// </summary>
public sealed class ScoreSyncJob
{
    public const string JobId = "score-sync";
    private const string Provider = "api_football";

    private readonly ISportsDataProvider _provider;
    private readonly IEnumerable<ISportsDataFallbackProvider> _fallbacks;
    private readonly AppDbContext _db;
    private readonly SyncRunTracker _tracker;
    private readonly CompetitionCatalogService _catalog;
    private readonly PredictionRescoreService _rescore;
    private readonly MatchweekBonusService _matchweekBonuses;
    private readonly ILogger<ScoreSyncJob> _logger;

    public ScoreSyncJob(
        ISportsDataProvider provider,
        IEnumerable<ISportsDataFallbackProvider> fallbacks,
        AppDbContext db,
        SyncRunTracker tracker,
        CompetitionCatalogService catalog,
        PredictionRescoreService rescore,
        MatchweekBonusService matchweekBonuses,
        ILogger<ScoreSyncJob> logger)
    {
        _provider = provider;
        _fallbacks = fallbacks;
        _db = db;
        _tracker = tracker;
        _catalog = catalog;
        _rescore = rescore;
        _matchweekBonuses = matchweekBonuses;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var added = 0;
        var updated = 0;

        try
        {
            var season = await _catalog.EnsureCurrentPremierLeagueAsync(cancellationToken);
            var all = await _provider.GetAllFixturesAsync(cancellationToken);
            var live = await _provider.GetLiveFixturesAsync(cancellationToken);

            if (all.Count == 0)
            {
                foreach (var fallback in _fallbacks.Where(f => f.IsConfigured))
                {
                    var fallbackFixtures = await fallback.GetFixturesAsync(cancellationToken);
                    if (fallbackFixtures.Count > 0)
                    {
                        all = fallbackFixtures;
                        await _tracker.LogErrorAsync(
                            Provider,
                            JobId,
                            "fixture",
                            $"Canonical fixtures empty; used fallback provider {fallback.ProviderName}.",
                            run.Id,
                            ct: cancellationToken);
                        break;
                    }
                }
            }

            if (all.Count == 0)
            {
                var hasPremierLeague = await _db.Matches.WherePremierLeague().AnyAsync(cancellationToken);
                if (!hasPremierLeague)
                {
                    all = await new MockSportsDataProvider().GetAllFixturesAsync(cancellationToken);
                    await _tracker.LogErrorAsync(
                        Provider,
                        JobId,
                        "fixture",
                        "Canonical fixtures empty; seeded mock Premier League fixtures.",
                        run.Id,
                        ct: cancellationToken);
                }
            }

            var merged = all
                .Concat(live)
                .GroupBy(d => d.Id)
                .Select(g => g.Last())
                .ToList();

            foreach (var dto in merged)
            {
                if (PremierLeagueMatchScope.IsWorldCupLegacyId(dto.Id))
                {
                    continue;
                }

                var match = await _db.Matches.FindAsync([dto.Id], cancellationToken);
                if (match is null)
                {
                    match = MatchMapper.FromDto(dto);
                    match.CompetitionSeasonId = season.Id;
                    _db.Matches.Add(match);
                    added++;
                }
                else if (MatchMapper.ApplyDto(match, dto))
                {
                    match.CompetitionSeasonId = season.Id;
                    updated++;
                }

                if (match.MatchweekNumber is int weekNumber)
                {
                    var week = await _catalog.EnsureMatchweekAsync(season, weekNumber, dto.KickoffUtc, cancellationToken);
                    match.MatchweekId = week.Id;
                    match.MatchweekNumber = weekNumber;
                }

                await _catalog.UpsertClubAsync(dto.HomeTeam.Code, dto.HomeTeam.Name, dto.HomeTeam.LogoUrl, dto.HomeTeam.Id, cancellationToken);
                await _catalog.UpsertClubAsync(dto.AwayTeam.Code, dto.AwayTeam.Name, dto.AwayTeam.LogoUrl, dto.AwayTeam.Id, cancellationToken);

                if (dto.Id.StartsWith("apifb-", StringComparison.OrdinalIgnoreCase) ||
                    dto.Id.StartsWith("pl26-", StringComparison.OrdinalIgnoreCase))
                {
                    var externalId = dto.Id.Contains('-')
                        ? dto.Id[(dto.Id.IndexOf('-') + 1)..]
                        : dto.Id;
                    await _tracker.UpsertExternalIdAsync(
                        "fixture",
                        dto.Id,
                        dto.Id.StartsWith("pl26-", StringComparison.OrdinalIgnoreCase) ? "mock" : Provider,
                        externalId,
                        ct: cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await PersistComputedStandingsAsync(cancellationToken);
            var rescored = await _rescore.RescoreFinishedMatchesAsync(cancellationToken);
            var bonuses = await _matchweekBonuses.AwardFinishedMatchweeksAsync(cancellationToken);

            await _tracker.CompleteAsync(run, added, updated, ct: cancellationToken);
            _logger.LogInformation(
                "Score sync: {Total} fixtures ({Live} live, {Added} added, {Updated} updated, {Rescored} rescored, {Bonuses} matchweek bonuses).",
                merged.Count,
                live.Count,
                added,
                updated,
                rescored,
                bonuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Score sync job failed.");
            await _tracker.FailAsync(run, added, updated, ex, cancellationToken);
        }
    }

    private async Task PersistComputedStandingsAsync(CancellationToken cancellationToken)
    {
        var plMatches = await _db.Matches.WherePremierLeague().ToListAsync(cancellationToken);
        var computed = PremierLeagueStandingsCalculator.FromMatches(plMatches);
        if (computed.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var row in computed)
        {
            var existing = await _db.StandingRows.FirstOrDefaultAsync(
                x => x.GroupKey == "PL" && x.TeamCode == row.TeamCode && x.Provider == Provider,
                cancellationToken);

            if (existing is null)
            {
                _db.StandingRows.Add(new StandingRow
                {
                    Id = Guid.NewGuid(),
                    CompetitionSeasonId = PremierLeagueCatalog.SeasonId,
                    GroupKey = "PL",
                    Rank = row.Rank,
                    TeamCode = row.TeamCode,
                    TeamName = row.TeamName,
                    LogoUrl = row.LogoUrl,
                    Played = row.Played,
                    Won = row.Won,
                    Drawn = row.Drawn,
                    Lost = row.Lost,
                    GoalsFor = row.GoalsFor,
                    GoalsAgainst = row.GoalsAgainst,
                    GoalDiff = row.GoalDiff,
                    Points = row.Points,
                    Provider = Provider,
                    LastSyncedAt = now
                });
            }
            else
            {
                existing.Rank = row.Rank;
                existing.TeamName = row.TeamName;
                existing.LogoUrl = row.LogoUrl ?? existing.LogoUrl;
                existing.Played = row.Played;
                existing.Won = row.Won;
                existing.Drawn = row.Drawn;
                existing.Lost = row.Lost;
                existing.GoalsFor = row.GoalsFor;
                existing.GoalsAgainst = row.GoalsAgainst;
                existing.GoalDiff = row.GoalDiff;
                existing.Points = row.Points;
                existing.LastSyncedAt = now;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
