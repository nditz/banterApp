using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Staggered job: seeds our first-party GIF library, refreshes football search phrases from
/// Giphy trending/tag endpoints (text only — not a Giphy GIF database), and optionally
/// upgrades a few stored stickers to live CDN URLs while under daily quota.
/// </summary>
public sealed class GifQueryRefreshJob
{
    public const string JobId = "gif-query-refresh";
    public const string UsageProvider = "giphy";

    private readonly AppDbContext _db;
    private readonly GifLibraryService _library;
    private readonly FeedReactionMediaService _feedMedia;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProviderUsageGuard _usage;
    private readonly SyncRunTracker _tracker;
    private readonly ReactionGifOptions _gifOptions;
    private readonly BackgroundJobsOptions _jobOptions;
    private readonly ILogger<GifQueryRefreshJob> _logger;

    public GifQueryRefreshJob(
        AppDbContext db,
        GifLibraryService library,
        FeedReactionMediaService feedMedia,
        IHttpClientFactory httpClientFactory,
        IProviderUsageGuard usage,
        SyncRunTracker tracker,
        IOptions<ReactionGifOptions> gifOptions,
        IOptions<BackgroundJobsOptions> jobOptions,
        ILogger<GifQueryRefreshJob> logger)
    {
        _db = db;
        _library = library;
        _feedMedia = feedMedia;
        _httpClientFactory = httpClientFactory;
        _usage = usage;
        _tracker = tracker;
        _gifOptions = gifOptions.Value;
        _jobOptions = jobOptions.Value;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var run = await _tracker.StartAsync(UsageProvider, JobId, cancellationToken);
        var created = 0;
        var updated = 0;
        try
        {
            await _library.EnsureSeededAsync(cancellationToken);
            created++;

            if (_gifOptions.IsGiphyEnabled)
            {
                updated += await RefreshGiphyQueriesAsync(cancellationToken);
            }

            var upgradeCap = Math.Clamp(_jobOptions.GifQueryRefreshStickerUpgrades, 0, 8);
            if (upgradeCap > 0 && await _usage.CanInvokeAsync(UsageProvider, 1, cancellationToken))
            {
                var stickers = await _db.NewsFeedItems
                    .Where(n => n.ImageUrl != null && n.ImageUrl.StartsWith("/reactions/"))
                    .OrderByDescending(n => n.PublishedAt)
                    .Take(upgradeCap)
                    .ToListAsync(cancellationToken);

                var upgraded = await _feedMedia.UpgradeStoredStickersAsync(
                    stickers,
                    cancellationToken,
                    maxUpgrades: upgradeCap);
                if (upgraded > 0)
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    updated += upgraded;
                }
            }

            await _tracker.CompleteAsync(run, created, updated, ct: cancellationToken);
            _logger.LogInformation(
                "GIF query refresh completed. Created {Created}, updated {Updated}.",
                created,
                updated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "GIF query refresh failed.");
            await _tracker.FailAsync(run, created, updated, ex, cancellationToken);
        }
    }

    private async Task<int> RefreshGiphyQueriesAsync(CancellationToken cancellationToken)
    {
        var maxCalls = Math.Clamp(_gifOptions.QueryRefreshMaxCalls, 1, 4);
        var calls = 0;
        var upserted = 0;
        var client = _httpClientFactory.CreateClient();

        if (calls < maxCalls && await _usage.CanInvokeAsync(UsageProvider, 1, cancellationToken))
        {
            var phrases = await FetchPhrasesAsync(
                client,
                $"{_gifOptions.GiphyBaseUrl.TrimEnd('/')}/trending/searches?api_key={Uri.EscapeDataString(_gifOptions.ApiKey!)}",
                cancellationToken);
            calls++;
            if (phrases.Count > 0)
            {
                var football = phrases.Where(FootballGifQuery.LooksFootball).ToList();
                await _library.UpsertQueriesAsync(football, GifSearchQuerySources.GiphyTrending, cancellationToken);
                upserted += football.Count;
            }
        }

        if (calls < maxCalls && await _usage.CanInvokeAsync(UsageProvider, 1, cancellationToken))
        {
            var phrases = await FetchPhrasesAsync(
                client,
                $"{_gifOptions.GiphyBaseUrl.TrimEnd('/')}/gifs/search/tags" +
                $"?api_key={Uri.EscapeDataString(_gifOptions.ApiKey!)}" +
                "&q=premier%20league",
                cancellationToken);
            calls++;
            if (phrases.Count > 0)
            {
                await _library.UpsertQueriesAsync(phrases, GifSearchQuerySources.GiphyTags, cancellationToken);
                upserted += phrases.Count;
            }
        }

        return upserted;
    }

    private async Task<IReadOnlyList<string>> FetchPhrasesAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            var latency = (int)(DateTime.UtcNow - started).TotalMilliseconds;
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                await _usage.RecordFailureAsync(
                    UsageProvider,
                    $"Giphy query refresh failed: {(int)response.StatusCode} {body}",
                    cancellationToken);
                _logger.LogWarning(
                    "Giphy query refresh HTTP {Status}: {Body}",
                    (int)response.StatusCode,
                    body);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            await _usage.RecordSuccessAsync(UsageProvider, 1, latency, cancellationToken);
            return GiphyResponseParser.ExtractSearchPhrases(doc.RootElement);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _usage.RecordFailureAsync(UsageProvider, ex.Message, cancellationToken);
            _logger.LogWarning(ex, "Giphy query refresh request failed.");
            return [];
        }
    }
}
