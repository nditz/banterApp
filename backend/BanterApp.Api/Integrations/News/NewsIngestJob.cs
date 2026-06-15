using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.SportsData;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.News;

/// <summary>
/// Longest-interval background job: ingests sports news, match fixtures, results,
/// and live score updates into the rolling news feed. Future: YouTube/podcast transcripts.
/// </summary>
public sealed class NewsIngestJob
{
    public const string JobId = "news-ingest";

    private readonly INewsProvider _news;
    private readonly ISportsDataProvider _sports;
    private readonly AppDbContext _db;
    private readonly NewsIngestOptions _options;
    private readonly ILogger<NewsIngestJob> _logger;

    public NewsIngestJob(
        INewsProvider news,
        ISportsDataProvider sports,
        AppDbContext db,
        IOptions<NewsIngestOptions> options,
        ILogger<NewsIngestJob> logger)
    {
        _news = news;
        _sports = sports;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task IngestAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var added = 0;
        var updated = 0;

        // 1. External sports news (NewsAPI / future scrapers)
        var articles = await _news.GetLatestArticlesAsync(_options.MaxArticlesPerRun, cancellationToken);
        foreach (var article in articles)
        {
            var (a, u) = await UpsertArticleAsync(article, cancellationToken);
            added += a;
            updated += u;
        }

        // 2. Tournament fixtures → rolling news
        if (_options.IncludeMatchFixtures)
        {
            var upcoming = await _sports.GetUpcomingFixturesAsync(cancellationToken);
            foreach (var match in upcoming.Take(10))
            {
                var id = $"match-fixture-{match.Id}";
                var title = $"Upcoming: {match.HomeTeam.Name} vs {match.AwayTeam.Name}";
                var summary =
                    $"Kickoff {match.KickoffUtc:ddd d MMM HH:mm} UTC · {match.Group} · {match.Venue}";
                var (a, u) = await UpsertMatchItemAsync(id, title, summary, "match_fixture", match.KickoffUtc, cancellationToken);
                added += a;
                updated += u;
            }
        }

        // 3. Full-time results
        if (_options.IncludeMatchResults)
        {
            var results = await _sports.GetResultsAsync(cancellationToken);
            foreach (var match in results.Take(10))
            {
                var id = $"match-result-{match.Id}";
                var score = match.HomeScore is not null && match.AwayScore is not null
                    ? $"{match.HomeScore}-{match.AwayScore}"
                    : "FT";
                var title = $"Full time: {match.HomeTeam.Name} {score} {match.AwayTeam.Name}";
                var summary = $"{match.Group} · {match.Stage} · {match.Venue}";
                var (a, u) = await UpsertMatchItemAsync(id, title, summary, "match_result", match.KickoffUtc, cancellationToken);
                added += a;
                updated += u;
            }
        }

        // 4. Live in-play scores (short-lived feed items)
        if (_options.IncludeLiveScores)
        {
            var live = await _sports.GetLiveFixturesAsync(cancellationToken);
            foreach (var match in live)
            {
                var id = $"match-live-{match.Id}";
                var score = match.HomeScore is not null && match.AwayScore is not null
                    ? $"{match.HomeScore}-{match.AwayScore}"
                    : "LIVE";
                var title = $"LIVE: {match.HomeTeam.Name} {score} {match.AwayTeam.Name}";
                var summary = $"In play · {match.Group} · {match.Status}";
                var (a, u) = await UpsertMatchItemAsync(id, title, summary, "match_live", DateTimeOffset.UtcNow, cancellationToken);
                added += a;
                updated += u;
            }
        }

        if (added > 0 || updated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "News ingest: {Articles} articles, match items ({Added} added, {Updated} updated). " +
            "YouTube channels configured: {Yt}, podcast feeds: {Pod}.",
            articles.Count,
            added,
            updated,
            _options.YouTubeChannelIds.Length,
            _options.PodcastFeedUrls.Length);
    }

    private async Task<(int Added, int Updated)> UpsertArticleAsync(
        NewsArticleDto article,
        CancellationToken ct)
    {
        var existing = await _db.NewsFeedItems.FindAsync([article.Id], ct);
        if (existing is null)
        {
            _db.NewsFeedItems.Add(new NewsFeedItem
            {
                Id = article.Id,
                Source = article.SourceName,
                Title = article.Title,
                Summary = article.Summary,
                Url = article.SourceUrl,
                ImageUrl = article.ImageUrl,
                Category = "sports_news",
                PublishedAt = article.PublishedAt,
                ViewCount = 0
            });
            return (1, 0);
        }

        var changed = existing.Title != article.Title ||
                        existing.Summary != article.Summary ||
                        existing.ImageUrl != article.ImageUrl;
        if (changed)
        {
            existing.Title = article.Title;
            existing.Summary = article.Summary;
            existing.ImageUrl = article.ImageUrl;
            existing.PublishedAt = article.PublishedAt;
            return (0, 1);
        }

        return (0, 0);
    }

    private async Task<(int Added, int Updated)> UpsertMatchItemAsync(
        string id,
        string title,
        string summary,
        string category,
        DateTimeOffset publishedAt,
        CancellationToken ct)
    {
        var existing = await _db.NewsFeedItems.FindAsync([id], ct);
        if (existing is null)
        {
            _db.NewsFeedItems.Add(new NewsFeedItem
            {
                Id = id,
                Source = "BanterApp Match Desk",
                Title = title,
                Summary = summary,
                Url = string.Empty,
                Category = category,
                PublishedAt = publishedAt,
                ViewCount = 0
            });
            return (1, 0);
        }

        if (existing.Title != title || existing.Summary != summary)
        {
            existing.Title = title;
            existing.Summary = summary;
            existing.PublishedAt = publishedAt;
            return (0, 1);
        }

        return (0, 0);
    }
}
