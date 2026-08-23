using BanterApp.Api.Common;
using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Feed;

public static class PunditOpinionFeedMapper
{
    public const string FeedCategory = "pundit_quote";
    public const string FeedItemIdPrefix = "pundit-opinion-";

    public static string FeedItemId(Guid opinionId) => $"{FeedItemIdPrefix}{opinionId:N}";

    public static NewsFeedItem ToNewsFeedItem(PunditOpinion opinion, Pundit pundit, MediaItem item)
    {
        var publication = item.Publication ?? item.MediaSource?.Name ?? "Unknown outlet";
        var matchLabel = opinion.Match is not null
            ? $"{opinion.Match.TeamA} vs {opinion.Match.TeamB}"
            : opinion.MatchName;

        return new NewsFeedItem
        {
            Id = FeedItemId(opinion.Id),
            Source = StringLimits.Truncate(publication, 120) ?? publication,
            Author = StringLimits.Truncate(pundit.Name, StringLimits.MediaAuthor) ?? pundit.Name,
            Title = BuildTitle(pundit.Name, opinion, matchLabel),
            Summary = BuildBody(opinion),
            Url = item.SourceUrl,
            Category = FeedCategory,
            PublishedAt = item.PublishedAt ?? opinion.CreatedAt,
            ViewCount = 0,
            MediaType = "gif",
            MatchId = opinion.MatchId,
            PredictionSummary = BuildPredictionSummary(opinion, pundit.Name)
        };
    }

    public static FeedItemResponse ToFeedItem(PunditOpinion opinion, Pundit pundit, MediaItem item)
    {
        var publication = item.Publication ?? item.MediaSource?.Name ?? "Unknown outlet";
        var matchLabel = opinion.MatchName;
        var media = FeedMediaMapper.FromGifMood("pundit", $"{pundit.Name} take", opinion.Id.GetHashCode());

        return new FeedItemResponse(
            FeedItemId(opinion.Id),
            "pundit_quote",
            BuildTitle(pundit.Name, opinion, matchLabel),
            BuildBody(opinion),
            media.Url,
            publication,
            item.SourceUrl,
            item.PublishedAt ?? opinion.CreatedAt,
            null,
            Reactions: null,
            Media: media,
            Author: pundit.Name,
            ContentLabel: opinion.IsDirectQuote ? "direct_quote" : "paraphrase",
            MatchId: opinion.MatchId,
            Prediction: opinion.Prediction,
            Confidence: opinion.Confidence);
    }

    public static string FormatSourceAttribution(string punditName, string publication) =>
        $"{punditName.Trim()} · {publication.Trim()}";

    private static string BuildTitle(string punditName, PunditOpinion opinion, string? matchLabel = null)
    {
        var matchPrefix = string.IsNullOrWhiteSpace(matchLabel) ? string.Empty : $"{matchLabel}: ";

        if (!string.IsNullOrWhiteSpace(opinion.Prediction))
        {
            var teamPrefix = string.IsNullOrWhiteSpace(opinion.Team) ? string.Empty : $"{opinion.Team}: ";
            return $"{punditName}: {matchPrefix}{teamPrefix}{StringLimits.Truncate(opinion.Prediction, 120)}";
        }

        if (!string.IsNullOrWhiteSpace(opinion.Team))
        {
            return $"{punditName} on {opinion.Team}";
        }

        var topic = string.IsNullOrWhiteSpace(opinion.Topic) ? "Premier League" : opinion.Topic;
        return $"{punditName} on {topic}";
    }

    private static string BuildBody(PunditOpinion opinion)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(opinion.Opinion))
        {
            parts.Add(opinion.Opinion.Trim());
        }

        if (!string.IsNullOrWhiteSpace(opinion.Prediction) &&
            !string.Equals(opinion.Prediction, opinion.Opinion, StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"Prediction: {opinion.Prediction.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(opinion.EvidenceQuote))
        {
            var quoteLabel = opinion.IsDirectQuote ? "Quote" : "Paraphrase";
            parts.Add($"{quoteLabel}: \"{opinion.EvidenceQuote.Trim()}\"");
        }

        return parts.Count > 0 ? string.Join("\n\n", parts) : "Pundit take extracted from source.";
    }

    private static string? BuildPredictionSummary(PunditOpinion opinion, string punditName)
    {
        if (string.IsNullOrWhiteSpace(opinion.Prediction))
        {
            return null;
        }

        return $"{punditName}: {opinion.Prediction.Trim()}";
    }
}
