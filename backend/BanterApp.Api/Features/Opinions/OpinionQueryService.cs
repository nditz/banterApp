using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Opinions;

public sealed class OpinionQueryService
{
    private readonly AppDbContext _db;

    public OpinionQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OpinionResponseDto>> QueryOpinionsAsync(
        string? team,
        string? source,
        string? player,
        bool? needsReview,
        DateTimeOffset? publishedAfter,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _db.PunditOpinions
            .AsNoTracking()
            .Include(o => o.Pundit)
            .Include(o => o.SourceItem)
            .ThenInclude(i => i.MediaSource)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(team))
        {
            query = query.Where(o => o.Team != null &&
                                     EF.Functions.ILike(o.Team, $"%{team.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(player))
        {
            query = query.Where(o => o.Player != null &&
                                     EF.Functions.ILike(o.Player, $"%{player.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            var sourceFilter = source.Trim();
            query = query.Where(o =>
                EF.Functions.ILike(o.SourceItem.Publication ?? o.SourceItem.MediaSource.Name, $"%{sourceFilter}%") ||
                EF.Functions.ILike(o.SourceItem.MediaSource.Name, $"%{sourceFilter}%"));
        }

        if (needsReview == true)
        {
            query = query.Where(o => o.NeedsHumanReview);
        }

        if (publishedAfter.HasValue)
        {
            query = query.Where(o => o.SourceItem.PublishedAt >= publishedAfter);
        }

        var rows = await query
            .OrderByDescending(o => o.SourceItem.PublishedAt ?? o.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows.Select(MapOpinion).ToList();
    }

    public async Task<IReadOnlyList<PunditSummaryDto>> QueryPunditsAsync(
        PunditKind kind,
        int take,
        CancellationToken cancellationToken)
    {
        return await _db.Pundits
            .AsNoTracking()
            .Where(p => p.Kind == kind)
            .Select(p => new PunditSummaryDto(
                p.Id,
                p.Name,
                p.Role,
                p.Organization,
                p.Opinions.Count))
            .OrderBy(p => p.Name)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpinionResponseDto>> GetPunditOpinionsAsync(
        Guid punditId,
        int take,
        CancellationToken cancellationToken)
    {
        var rows = await _db.PunditOpinions
            .AsNoTracking()
            .Include(o => o.Pundit)
            .Include(o => o.SourceItem)
            .ThenInclude(i => i.MediaSource)
            .Where(o => o.PunditId == punditId)
            .OrderByDescending(o => o.SourceItem.PublishedAt ?? o.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows.Select(MapOpinion).ToList();
    }

    public async Task<IReadOnlyList<SourceSummaryDto>> QuerySourcesAsync(CancellationToken cancellationToken)
    {
        var sources = await _db.MediaSources
            .AsNoTracking()
            .Where(s => s.ExtractPredictions && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.SourceType,
                Url = s.RssUrl ?? s.SiteUrl,
                s.IsActive,
                ItemCount = s.Items.Count
            })
            .ToListAsync(cancellationToken);

        return sources
            .Select(s => new SourceSummaryDto(
                s.Id,
                s.Name,
                s.SourceType,
                s.Url,
                s.IsActive,
                s.ItemCount))
            .ToList();
    }

    public async Task<SourceItemDetailDto?> GetSourceItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _db.MediaItems
            .AsNoTracking()
            .Include(i => i.MediaSource)
            .Include(i => i.Opinions)
            .ThenInclude(o => o.Pundit)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        var opinions = item.Opinions
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapOpinion(o, item))
            .ToList();

        return new SourceItemDetailDto(
            item.Id,
            item.Title,
            item.SourceUrl,
            item.Author,
            item.Publication ?? item.MediaSource.Name,
            item.PublishedAt,
            item.ProcessingStatus,
            opinions);
    }

    public async Task<IReadOnlyList<PunditPredictionAggregateDto>> QueryPredictionAggregatesAsync(
        string? team,
        string? entityType,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _db.PredictionAggregates.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(team))
        {
            query = query.Where(a => a.EntityType == "team" &&
                                     EF.Functions.ILike(a.EntityName, $"%{team.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType.Trim().ToLowerInvariant());
        }

        return await query
            .OrderByDescending(a => a.UpdatedAt)
            .Take(take)
            .Select(a => new PunditPredictionAggregateDto(
                a.EntityType,
                a.EntityName,
                a.PredictionType,
                a.ConsensusSummary,
                a.PositiveCount,
                a.NegativeCount,
                a.NeutralCount,
                a.SourceCount,
                a.ConfidenceScore,
                a.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    private static OpinionResponseDto MapOpinion(PunditOpinion opinion) =>
        MapOpinion(opinion, opinion.SourceItem);

    private static OpinionResponseDto MapOpinion(PunditOpinion opinion, MediaItem item) =>
        new(
            opinion.Id,
            opinion.Pundit.Name,
            opinion.Opinion,
            opinion.Prediction,
            opinion.Team,
            opinion.Player,
            item.Publication ?? item.MediaSource.Name,
            item.Title,
            item.SourceUrl,
            item.PublishedAt,
            opinion.EvidenceQuote,
            opinion.IsDirectQuote,
            opinion.Confidence,
            opinion.NeedsHumanReview);
}
