using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Pundits.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class PunditOpinionPersistenceService
{
    private static readonly HashSet<string> JournalistRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "journalist",
        "reporter",
        "columnist",
        "writer",
        "editor"
    };

    private readonly AppDbContext _db;
    private readonly PunditReviewFlagger _reviewFlagger;
    private readonly ReactionMediaResolver _reactionMedia;
    private readonly MatchResolutionService _matchResolution;
    private readonly FeedRelevanceScorer _relevanceScorer;
    private readonly ProcessingOptions _processing;
    private readonly MediaIngestOptions _mediaIngest;

    public PunditOpinionPersistenceService(
        AppDbContext db,
        PunditReviewFlagger reviewFlagger,
        ReactionMediaResolver reactionMedia,
        MatchResolutionService matchResolution,
        FeedRelevanceScorer relevanceScorer,
        IOptions<ProcessingOptions> processing,
        IOptions<MediaIngestOptions> mediaIngest)
    {
        _db = db;
        _reviewFlagger = reviewFlagger;
        _reactionMedia = reactionMedia;
        _matchResolution = matchResolution;
        _relevanceScorer = relevanceScorer;
        _processing = processing.Value;
        _mediaIngest = mediaIngest.Value;
    }

    public async Task<int> PersistExtractionAsync(
        MediaItem item,
        PunditExtractionResult extraction,
        CancellationToken cancellationToken)
    {
        if (item.MediaSource is null)
        {
            await _db.Entry(item).Reference(i => i.MediaSource).LoadAsync(cancellationToken);
        }

        var sourceTextLength = item.RawText?.Length ?? 0;
        var distinctPundits = extraction.Pundits.Count;
        var created = 0;
        var confidenceScoring = ConfidenceScoringHelper.ResolveForSource(item.MediaSource!, _mediaIngest);
        var predictionsThisSource = 0;

        foreach (var punditDto in extraction.Pundits)
        {
            var punditName = ResolvePunditName(punditDto, item);
            var pundit = await UpsertSourcePunditAsync(
                new PunditExtractionPunditDto(punditName, punditDto.Role, punditDto.Opinions),
                item,
                cancellationToken);

            foreach (var opinionDto in punditDto.Opinions)
            {
                if (predictionsThisSource >= _processing.PredictionExtraction.MaxPredictionsPerSource)
                {
                    break;
                }

                var adjustedConfidence = ConfidenceScoringHelper.AdjustConfidence(
                    opinionDto.Confidence,
                    $"{opinionDto.Opinion} {opinionDto.Prediction}",
                    confidenceScoring);

                var resolution = await ResolveMatchAsync(opinionDto, cancellationToken);
                var matchId = resolution.MatchId;
                if (matchId is null &&
                    !string.IsNullOrWhiteSpace(opinionDto.MatchId) &&
                    await _db.Matches.AnyAsync(m => m.Id == opinionDto.MatchId, cancellationToken))
                {
                    matchId = opinionDto.MatchId;
                    resolution = resolution with { MatchId = matchId, Confidence = Math.Max(resolution.Confidence, 0.8) };
                }

                var enrichedOpinion = opinionDto with { Confidence = adjustedConfidence };
                var needsReview = _reviewFlagger.ShouldReviewOpinion(
                    enrichedOpinion,
                    punditName,
                    sourceTextLength,
                    distinctPundits,
                    punditDto.Role);

                if (resolution.Confidence < _processing.PredictionExtraction.ConfidenceThreshold &&
                    MatchResolutionService.IsMatchLevelPrediction(opinionDto.PredictionType))
                {
                    needsReview = true;
                }

                var opinion = new PunditOpinion
                {
                    Id = Guid.NewGuid(),
                    SourceItemId = item.Id,
                    PunditId = pundit.Id,
                    Topic = StringLimits.Truncate(opinionDto.Topic, StringLimits.OpinionTopic),
                    Team = StringLimits.Truncate(opinionDto.Team ?? resolution.TeamA, StringLimits.OpinionTeam),
                    Player = StringLimits.Truncate(opinionDto.Player, StringLimits.OpinionPlayer),
                    MatchName = StringLimits.Truncate(
                        opinionDto.Match ?? BuildMatchLabel(resolution),
                        StringLimits.OpinionMatchName),
                    MatchId = matchId,
                    Opinion = opinionDto.Opinion,
                    Prediction = opinionDto.Prediction,
                    PredictionType = StringLimits.Truncate(opinionDto.PredictionType, StringLimits.PredictionType),
                    Confidence = adjustedConfidence,
                    EvidenceQuote = opinionDto.EvidenceQuote,
                    QuoteContext = opinionDto.QuoteContext,
                    IsDirectQuote = opinionDto.IsDirectQuote,
                    NeedsHumanReview = needsReview,
                    ExtractedJson = extraction.RawJson,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.PunditOpinions.Add(opinion);

                if (matchId is not null &&
                    MatchResolutionService.IsMatchLevelPrediction(opinionDto.PredictionType) &&
                    !string.IsNullOrWhiteSpace(opinionDto.Prediction))
                {
                    await UpsertPunditPredictionAsync(opinion, pundit, item, cancellationToken);
                }

                if (!needsReview)
                {
                    await UpsertFeedItemAsync(opinion, pundit, item, cancellationToken);
                }

                created++;
                predictionsThisSource++;
            }
        }

        item.ProcessedAt = DateTimeOffset.UtcNow;
        item.ProcessingStatus = MediaItemProcessingStatus.Extracted;
        item.ProcessingError = null;
        return created;
    }

    private async Task<MatchResolutionResult> ResolveMatchAsync(
        PunditExtractionOpinionDto opinionDto,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(opinionDto.MatchId))
        {
            var exists = await _db.Matches.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == opinionDto.MatchId, cancellationToken);
            if (exists is not null)
            {
                return new MatchResolutionResult(exists.Id, exists.TeamA, exists.TeamB, 0.95);
            }
        }

        return await _matchResolution.ResolveAsync(opinionDto.Match, opinionDto.Team, cancellationToken);
    }

    private static string ResolvePunditName(PunditExtractionPunditDto dto, MediaItem item)
    {
        if (!string.IsNullOrWhiteSpace(dto.Name) &&
            !string.Equals(dto.Name, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return dto.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(item.Author))
        {
            return item.Author.Trim();
        }

        return dto.Name;
    }

    private async Task UpsertPunditPredictionAsync(
        PunditOpinion opinion,
        Pundit pundit,
        MediaItem item,
        CancellationToken cancellationToken)
    {
        var existing = await _db.PunditPredictions
            .FirstOrDefaultAsync(
                p => p.PunditId == pundit.Id &&
                     p.MatchId == opinion.MatchId &&
                     p.PredictionType == opinion.PredictionType,
                cancellationToken);

        var predictedTeam = opinion.Team;
        var predictedScore = ExtractScore(opinion.Prediction);

        if (existing is null)
        {
            _db.PunditPredictions.Add(new PunditPrediction
            {
                Id = Guid.NewGuid(),
                PunditId = pundit.Id,
                MatchId = opinion.MatchId!,
                Prediction = opinion.Prediction ?? opinion.Opinion,
                PublishedAt = item.PublishedAt ?? opinion.CreatedAt,
                SourceType = item.MediaSource?.SourceType,
                SourceUrl = item.SourceUrl,
                Author = pundit.Name,
                Speaker = pundit.Name,
                PredictionType = opinion.PredictionType,
                PredictedTeam = predictedTeam,
                PredictedScore = predictedScore,
                Confidence = opinion.Confidence,
                EvidenceSnippet = opinion.EvidenceQuote,
                IsMatched = true
            });
            return;
        }

        existing.Prediction = opinion.Prediction ?? opinion.Opinion;
        existing.PublishedAt = item.PublishedAt ?? opinion.CreatedAt;
        existing.SourceUrl = item.SourceUrl;
        existing.PredictedTeam = predictedTeam;
        existing.PredictedScore = predictedScore;
        existing.Confidence = opinion.Confidence;
        existing.EvidenceSnippet = opinion.EvidenceQuote;
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

        var role = JournalistRoles.Contains(dto.Role) ? dto.Role : dto.Role;

        if (existing is not null)
        {
            existing.Role = StringLimits.Truncate(role, StringLimits.PunditRole) ?? existing.Role;
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
            Role = StringLimits.Truncate(role, StringLimits.PunditRole),
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

        if (opinion.MatchId is not null)
        {
            await _db.Entry(opinion).Reference(o => o.Match).LoadAsync(cancellationToken);
        }

        var feedItem = PunditOpinionFeedMapper.ToNewsFeedItem(opinion, pundit, item);
        var duplicateExists = await _db.NewsFeedItems
            .AnyAsync(
                n => n.Url == feedItem.Url &&
                     n.Category == PunditOpinionFeedMapper.FeedCategory &&
                     n.Id != feedItem.Id,
                cancellationToken);

        var relevance = _relevanceScorer.Score(
            feedItem,
            opinion,
            item.MediaSource,
            duplicateExists);
        feedItem.QualityScore = relevance.Score;

        var title = FeedBanterFormat.Strip(feedItem.Title);
        var queries = FeedReactionMediaService.BuildSearchQueries(
            title,
            feedItem.Summary,
            pundit.Name,
            feedItem.Category);
        var media = await _reactionMedia.ResolveAsync(
            queries,
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
        existing.MatchId = feedItem.MatchId;
        existing.QualityScore = feedItem.QualityScore;
        existing.PredictionSummary = feedItem.PredictionSummary;
    }

    private static string? BuildMatchLabel(MatchResolutionResult resolution)
    {
        if (resolution.TeamA is null || resolution.TeamB is null)
        {
            return null;
        }

        return $"{resolution.TeamA} vs {resolution.TeamB}";
    }

    private static string? ExtractScore(string? prediction)
    {
        if (string.IsNullOrWhiteSpace(prediction))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(prediction, @"\b(\d+)\s*[-–]\s*(\d+)\b");
        return match.Success ? match.Value : null;
    }
}
