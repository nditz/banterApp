using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Banter;

public sealed class BanterHistoryService : IBanterHistoryService
{
    private readonly AppDbContext _db;
    private readonly BanterOptions _options;
    private readonly ILogger<BanterHistoryService> _logger;

    public BanterHistoryService(
        AppDbContext db,
        IOptions<BanterOptions> options,
        ILogger<BanterHistoryService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BanterExclusionContext> GetExclusionsAsync(
        BanterContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var userSince = now.AddDays(-_options.RecentContentWindowDays);
        var teamSince = now.AddDays(-_options.RecentTeamContentWindowDays);
        var globalSince = now.AddDays(-_options.GlobalHardRepeatWindowDays);

        var rows = await _db.BanterContentHistories
            .AsNoTracking()
            .Where(h =>
                h.UsedAtUtc >= globalSince ||
                (context.UserId.HasValue && h.UserId == context.UserId && h.UsedAtUtc >= userSince) ||
                (!string.IsNullOrWhiteSpace(context.TeamId) &&
                 h.TeamId == context.TeamId &&
                 h.UsedAtUtc >= teamSince))
            .Select(h => new
            {
                h.UserId,
                h.TeamId,
                h.ProviderContentId,
                h.MemeTemplateId,
                h.SearchPhrase,
                h.CaptionHash,
                h.UsedAtUtc
            })
            .ToListAsync(cancellationToken);

        var exclusions = new BanterExclusionContext();

        foreach (var row in rows)
        {
            var inUserWindow = context.UserId.HasValue &&
                               row.UserId == context.UserId &&
                               row.UsedAtUtc >= userSince;
            var inTeamWindow = !string.IsNullOrWhiteSpace(context.TeamId) &&
                               string.Equals(row.TeamId, context.TeamId, StringComparison.Ordinal) &&
                               row.UsedAtUtc >= teamSince;
            var inGlobalWindow = row.UsedAtUtc >= globalSince;

            if (!inUserWindow && !inTeamWindow && !inGlobalWindow)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.ProviderContentId))
            {
                exclusions.ProviderContentIds.Add(row.ProviderContentId);
            }

            if (!string.IsNullOrWhiteSpace(row.MemeTemplateId) && (inUserWindow || inTeamWindow))
            {
                exclusions.MemeTemplateIds.Add(row.MemeTemplateId);
            }

            if (!string.IsNullOrWhiteSpace(row.SearchPhrase) && (inUserWindow || inTeamWindow))
            {
                exclusions.SearchPhrases.Add(BanterExclusionContext.NormalizePhrase(row.SearchPhrase));
            }

            if (!string.IsNullOrWhiteSpace(row.CaptionHash) && inUserWindow)
            {
                exclusions.CaptionHashes.Add(row.CaptionHash);
            }
        }

        return exclusions;
    }

    public async Task RecordAsync(BanterSelection selection, CancellationToken cancellationToken = default)
    {
        try
        {
            _db.BanterContentHistories.Add(new BanterContentHistory
            {
                Id = Guid.NewGuid(),
                UserId = selection.Context.UserId,
                MatchId = StringLimits.Truncate(selection.Context.MatchId, BanterContentHistoryLimits.MatchId),
                TeamId = StringLimits.Truncate(selection.Context.TeamId, BanterContentHistoryLimits.TeamId),
                PredictionId = selection.Context.PredictionId,
                ScenarioType = StringLimits.Truncate(selection.Scenario.ToString(), BanterContentHistoryLimits.ScenarioType)!,
                ContentType = StringLimits.Truncate(selection.ContentType, BanterContentHistoryLimits.ContentType)!,
                Provider = StringLimits.Truncate(selection.Provider, BanterContentHistoryLimits.Provider)!,
                ProviderContentId = StringLimits.Truncate(
                    selection.ProviderContentId,
                    BanterContentHistoryLimits.ProviderContentId),
                SearchPhrase = StringLimits.Truncate(selection.SearchPhrase, BanterContentHistoryLimits.SearchPhrase),
                MemeTemplateId = StringLimits.Truncate(
                    selection.MemeTemplateId,
                    BanterContentHistoryLimits.MemeTemplateId),
                CaptionHash = StringLimits.Truncate(selection.CaptionHash, BanterContentHistoryLimits.CaptionHash),
                SelectionScore = selection.SelectionScore,
                UsedAtUtc = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // History must not break the user journey.
            _logger.LogWarning(ex, "Banter history persistence failed; continuing without blocking generation.");
        }
    }
}
