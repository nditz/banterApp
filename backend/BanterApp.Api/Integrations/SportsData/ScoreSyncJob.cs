using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.SportsData.Dtos;
using BanterApp.Api.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Hangfire recurring job that polls the configured sports data provider
/// and upserts fixtures/scores into the database.
/// </summary>
public sealed class ScoreSyncJob
{
    public const string JobId = "score-sync";

    private readonly ISportsDataProvider _provider;
    private readonly IEnumerable<ISportsDataFallbackProvider> _fallbacks;
    private readonly AppDbContext _db;
    private readonly SyncRunTracker _tracker;
    private readonly CompetitionCatalogService _catalog;
    private readonly PredictionRescoreService _rescore;
    private readonly MatchweekBonusService _matchweekBonuses;
    private readonly SportsDataOptions _options;
    private readonly ILogger<ScoreSyncJob> _logger;

    public ScoreSyncJob(
        ISportsDataProvider provider,
        IEnumerable<ISportsDataFallbackProvider> fallbacks,
        AppDbContext db,
        SyncRunTracker tracker,
        CompetitionCatalogService catalog,
        PredictionRescoreService rescore,
        MatchweekBonusService matchweekBonuses,
        IOptions<SportsDataOptions> options,
        ILogger<ScoreSyncJob> logger)
    {
        _provider = provider;
        _fallbacks = fallbacks;
        _db = db;
        _tracker = tracker;
        _catalog = catalog;
        _rescore = rescore;
        _matchweekBonuses = matchweekBonuses;
        _options = options.Value;
        _logger = logger;
    }

    private string SyncProviderName =>
        FootballDatasetStatus.IsFootballDataProvider(_options.Provider)
            ? "football_data"
            : FootballDatasetStatus.IsMockProvider(_options.Provider)
                ? "mock"
                : "api_football";

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var run = await _tracker.StartAsync(SyncProviderName, JobId, cancellationToken);
        var added = 0;
        var updated = 0;

        try
        {
            var season = await _catalog.EnsureCurrentPremierLeagueAsync(cancellationToken);
            IReadOnlyList<MatchDto> all = [];
            try
            {
                all = await _provider.GetAllFixturesAsync(cancellationToken);
            }
            catch (SportsDataUnavailableException ex)
            {
                _logger.LogWarning(ex, "Canonical sports provider returned no fixtures.");
            }

            IReadOnlyList<MatchDto> live = [];
            try
            {
                live = await _provider.GetLiveFixturesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live fixture poll failed; continuing with the full fixture list.");
            }

            var usingMock = FootballDatasetStatus.IsMockProvider(_options.Provider);

            if (all.Count == 0)
            {
                foreach (var fallback in _fallbacks.Where(f => f.IsConfigured))
                {
                    var fallbackFixtures = await fallback.GetFixturesAsync(cancellationToken);
                    if (fallbackFixtures.Count > 0)
                    {
                        all = fallbackFixtures;
                        _logger.LogWarning(
                            "Canonical fixtures empty; using fallback provider {Fallback} ({Count} fixtures).",
                            fallback.ProviderName,
                            fallbackFixtures.Count);
                        break;
                    }
                }
            }

            if (all.Count == 0)
            {
                var hasPremierLeague = await _db.Matches.WherePremierLeague().AnyAsync(cancellationToken);
                if (usingMock && !hasPremierLeague)
                {
                    all = await new MockSportsDataProvider().GetAllFixturesAsync(cancellationToken);
                    await _tracker.LogErrorAsync(
                        SyncProviderName,
                        JobId,
                        "fixture",
                        "Canonical fixtures empty; seeded mock Premier League fixtures.",
                        run.Id,
                        ct: cancellationToken);
                }
                else if (!usingMock)
                {
                    throw new SportsDataUnavailableException(
                        "Score sync fetched 0 Premier League fixtures from the live sports provider.");
                }
            }

            // Never stamp foreign competitions as PL — only league-scoped / Group==PL / pl26-* rows.
            var merged = all
                .Concat(live)
                .Where(PremierLeagueMatchScope.IsPremierLeagueDto)
                .GroupBy(d => d.Id)
                .Select(g => g.Last())
                .ToList();

            if (merged.Count == 0 && !usingMock)
            {
                throw new SportsDataUnavailableException(
                    "Score sync fetched 0 Premier League fixtures from the live sports provider.");
            }

            foreach (var dto in merged)
            {
                var match = await _db.Matches.FindAsync([dto.Id], cancellationToken);
                if (match is null)
                {
                    match = MatchMapper.FromDto(dto);
                    match.CompetitionSeasonId = season.Id;
                    match.Group = "PL";
                    _db.Matches.Add(match);
                    added++;
                }
                else if (MatchMapper.ApplyDto(match, dto) ||
                         match.CompetitionSeasonId != season.Id ||
                         !string.Equals(match.Group, "PL", StringComparison.OrdinalIgnoreCase))
                {
                    match.CompetitionSeasonId = season.Id;
                    match.Group = "PL";
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
                    dto.Id.StartsWith("fd-", StringComparison.OrdinalIgnoreCase) ||
                    dto.Id.StartsWith("pl26-", StringComparison.OrdinalIgnoreCase))
                {
                    var externalId = dto.Id.Contains('-')
                        ? dto.Id[(dto.Id.IndexOf('-') + 1)..]
                        : dto.Id;
                    var idProvider = dto.Id.StartsWith("pl26-", StringComparison.OrdinalIgnoreCase)
                        ? "mock"
                        : dto.Id.StartsWith("fd-", StringComparison.OrdinalIgnoreCase)
                            ? "football_data"
                            : SyncProviderName;
                    await _tracker.UpsertExternalIdAsync(
                        "fixture",
                        dto.Id,
                        idProvider,
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
            throw;
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
                x => x.GroupKey == "PL" && x.TeamCode == row.TeamCode && x.Provider == SyncProviderName,
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
                    Provider = SyncProviderName,
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
