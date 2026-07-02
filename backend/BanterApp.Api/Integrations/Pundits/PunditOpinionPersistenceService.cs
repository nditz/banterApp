using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Pundits.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class PunditOpinionPersistenceService
{
    private readonly AppDbContext _db;
    private readonly PunditReviewFlagger _reviewFlagger;
    private readonly ReactionMediaResolver _reactionMedia;

    public PunditOpinionPersistenceService(
        AppDbContext db,
        PunditReviewFlagger reviewFlagger,
        ReactionMediaResolver reactionMedia)
    {
        _db = db;
        _reviewFlagger = reviewFlagger;
        _reactionMedia = reactionMedia;
    }

    public async Task<int> PersistExtractionAsync(
        MediaItem item,
        PunditExtractionResult extraction,
        CancellationToken cancellationToken)
    {
        var sourceTextLength = item.RawText?.Length ?? 0;
        var distinctPundits = extraction.Pundits.Count;
        var created = 0;

        foreach (var punditDto in extraction.Pundits)
        {
            var pundit = await UpsertSourcePunditAsync(punditDto, item, cancellationToken);

            foreach (var opinionDto in punditDto.Opinions)
            {
                var needsReview = _reviewFlagger.ShouldReviewOpinion(
                    opinionDto,
                    punditDto.Name,
                    sourceTextLength,
                    distinctPundits);

                var opinion = new PunditOpinion
                {
                    Id = Guid.NewGuid(),
                    SourceItemId = item.Id,
                    PunditId = pundit.Id,
                    Topic = StringLimits.Truncate(opinionDto.Topic, StringLimits.OpinionTopic),
                    Team = StringLimits.Truncate(opinionDto.Team, StringLimits.OpinionTeam),
                    Player = StringLimits.Truncate(opinionDto.Player, StringLimits.OpinionPlayer),
                    MatchName = StringLimits.Truncate(opinionDto.Match, StringLimits.OpinionMatchName),
                    Opinion = opinionDto.Opinion,
                    Prediction = opinionDto.Prediction,
                    PredictionType = StringLimits.Truncate(opinionDto.PredictionType, StringLimits.PredictionType),
                    Confidence = opinionDto.Confidence,
                    EvidenceQuote = opinionDto.EvidenceQuote,
                    QuoteContext = opinionDto.QuoteContext,
                    IsDirectQuote = opinionDto.IsDirectQuote,
                    NeedsHumanReview = needsReview,
                    ExtractedJson = extraction.RawJson,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.PunditOpinions.Add(opinion);

                if (!needsReview)
                {
                    await UpsertFeedItemAsync(opinion, pundit, item, cancellationToken);
                }

                created++;
            }
        }

        item.ProcessedAt = DateTimeOffset.UtcNow;
        item.ProcessingStatus = MediaItemProcessingStatus.Extracted;
        item.ProcessingError = null;
        return created;
    }

    private async Task<Pundit> UpsertSourcePunditAsync(
        PunditExtractionPunditDto dto,
        MediaItem item,
        CancellationToken cancellationToken)
    {
        var normalized = dto.Name.Trim().ToLowerInvariant();
        var existing = await _db.Pundits
            .FirstOrDefaultAsync(
                p => p.Kind == PunditKind.Source && p.NormalizedName == normalized,
                cancellationToken);

        if (existing is not null)
        {
            existing.Role = StringLimits.Truncate(dto.Role, StringLimits.PunditRole) ?? existing.Role;
            if (string.IsNullOrWhiteSpace(existing.Organization))
            {
                existing.Organization = item.Publication ?? item.MediaSource.Name;
            }

            return existing;
        }

        var pundit = new Pundit
        {
            Id = Guid.NewGuid(),
            Kind = PunditKind.Source,
            Name = dto.Name,
            NormalizedName = StringLimits.Truncate(normalized, StringLimits.PunditNormalizedName) ?? normalized,
            Role = StringLimits.Truncate(dto.Role, StringLimits.PunditRole),
            Organization = item.Publication ?? item.MediaSource.Name,
            AttributionMode = PunditAttributionMode.Licensed,
            SourceUrl = item.SourceUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Pundits.Add(pundit);
        return pundit;
    }

    private async Task UpsertFeedItemAsync(
        PunditOpinion opinion,
        Pundit pundit,
        MediaItem item,
        CancellationToken cancellationToken)
    {
        if (item.MediaSource is null)
        {
            await _db.Entry(item).Reference(i => i.MediaSource).LoadAsync(cancellationToken);
        }

        var feedItem = PunditOpinionFeedMapper.ToNewsFeedItem(opinion, pundit, item);
        var media = await _reactionMedia.ResolveAsync(
            null,
            "pundit",
            feedItem.Id.GetHashCode(),
            cancellationToken);
        feedItem.ImageUrl = media.Url;
        feedItem.MediaType = media.Type;

        var existing = await _db.NewsFeedItems.FindAsync([feedItem.Id], cancellationToken);
        if (existing is null)
        {
            _db.NewsFeedItems.Add(feedItem);
            return;
        }

        existing.Source = feedItem.Source;
        existing.Author = feedItem.Author;
        existing.Title = feedItem.Title;
        existing.Summary = feedItem.Summary;
        existing.Url = feedItem.Url;
        existing.Category = feedItem.Category;
        existing.PublishedAt = feedItem.PublishedAt;
        existing.ImageUrl = feedItem.ImageUrl;
        existing.MediaType = feedItem.MediaType;
    }
}
