using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.Pundits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Data;

/// <summary>
/// One-shot helper to remove stub extractions, World Cup leftovers, and pundit
/// rows invented from titles/descriptions. Not invoked on API startup.
/// </summary>
public static class UnsourcedPunditPurge
{
    public static async Task<int> ExecuteAsync(
        AppDbContext db,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var opinions = await db.PunditOpinions
            .Include(o => o.Match)
            .Include(o => o.SourceItem)
            .ToListAsync(cancellationToken);

        var junkOpinions = opinions.Where(IsJunkOpinion).ToList();
        var opinionIds = junkOpinions.Select(o => o.Id).ToHashSet();

        var predictions = await db.PunditPredictions
            .Include(p => p.Match)
            .ToListAsync(cancellationToken);
        var junkPredictions = predictions
            .Where(p => p.Match is null || !PremierLeagueMatchScope.IsPremierLeague(p.Match))
            .ToList();

        var feedIds = opinionIds
            .Select(PunditOpinionFeedMapper.FeedItemId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var junkFeed = await db.NewsFeedItems
            .Where(n =>
                n.Category == PunditOpinionFeedMapper.FeedCategory &&
                (feedIds.Contains(n.Id) ||
                 LooksLikeWorldCupText(n.Title) ||
                 LooksLikeWorldCupText(n.Url) ||
                 LooksLikeWorldCupText(n.Summary)))
            .ToListAsync(cancellationToken);

        db.NewsFeedItems.RemoveRange(junkFeed);
        db.PunditPredictions.RemoveRange(junkPredictions);
        db.PunditOpinions.RemoveRange(junkOpinions);

        var incompleteMedia = await db.MediaItems
            .Where(i =>
                i.ProcessingError != null &&
                i.ProcessingError.Contains("Transcript incomplete"))
            .ToListAsync(cancellationToken);
        foreach (var item in incompleteMedia)
        {
            item.ProcessingStatus = MediaItemProcessingStatus.Skipped;
            item.ProcessingError = SourceTextQuality.IncompleteTranscriptMessage;
            item.RawText = null;
        }

        await db.SaveChangesAsync(cancellationToken);

        var orphanPundits = await db.Pundits
            .Where(p => p.Kind == PunditKind.Source)
            .Where(p => !p.Opinions.Any() && !p.Predictions.Any())
            .ToListAsync(cancellationToken);
        db.Pundits.RemoveRange(orphanPundits);

        await db.SaveChangesAsync(cancellationToken);

        var removed = junkOpinions.Count + junkPredictions.Count + orphanPundits.Count;
        if (removed > 0 || junkFeed.Count > 0)
        {
            logger?.LogWarning(
                "Unsourced pundit purge: {Opinions} opinions, {Predictions} predictions, {Pundits} pundits, {Feed} feed items.",
                junkOpinions.Count,
                junkPredictions.Count,
                orphanPundits.Count,
                junkFeed.Count);
        }

        return removed;
    }

    private static bool IsJunkOpinion(PunditOpinion opinion)
    {
        if (opinion.Opinion.StartsWith("Stub summary", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(opinion.ExtractedJson, "{}", StringComparison.Ordinal))
        {
            return true;
        }

        if (opinion.Match is null || !PremierLeagueMatchScope.IsPremierLeague(opinion.Match))
        {
            return true;
        }

        var source = opinion.SourceItem;
        if (source is not null &&
            (LooksLikeWorldCupText(source.Title) ||
             LooksLikeWorldCupText(source.SourceUrl) ||
             LooksLikeWorldCupText(source.Description) ||
             (source.ProcessingError?.Contains("Transcript incomplete", StringComparison.OrdinalIgnoreCase) ?? false) ||
             SourceTextQuality.IsTitleDescriptionFallback(source.Title, source.Description, source.RawText)))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeWorldCupText(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.Contains("world cup", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("world-cup", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("worldcup", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("fifa", StringComparison.OrdinalIgnoreCase));
}
