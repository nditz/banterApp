using BanterApp.Api.Data;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.FootballBanter;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Rss;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class RssOpinionSyncJob
{
    public const string JobId = "rss-opinion-sync";
    private const string Provider = "pundit-rss";

    private readonly AppDbContext _db;
    private readonly IRssFeedProvider _rss;
    private readonly IRssFeedCatalog _catalog;
    private readonly RssFeedResolver _resolver;
    private readonly PunditMediaItemService _mediaItems;
    private readonly PunditIngestOptions _options;
    private readonly IFootballBanterConfigProvider _banterConfig;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<RssOpinionSyncJob> _logger;

    public RssOpinionSyncJob(
        AppDbContext db,
        IRssFeedProvider rss,
        IRssFeedCatalog catalog,
        RssFeedResolver resolver,
        PunditMediaItemService mediaItems,
        IOptions<PunditIngestOptions> options,
        IFootballBanterConfigProvider banterConfig,
        SyncRunTracker tracker,
        ILogger<RssOpinionSyncJob> logger)
    {
        _db = db;
        _rss = rss;
        _catalog = catalog;
        _resolver = resolver;
        _mediaItems = mediaItems;
        _options = options.Value;
        _banterConfig = banterConfig;
        _tracker = tracker;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    [DisableConcurrentExecution(60 * 30)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var created = 0;
        var updated = 0;
        var failed = 0;

        try
        {
            await RefreshFeedUrlsAsync(cancellationToken);

            foreach (var feed in await ResolvePunditFeedsAsync(cancellationToken))
            {
                var url = feed.Url;
                var publication = feed.Name;
                var feedCreated = 0;
                var feedUpdated = 0;

                try
                {
                    var source = await _mediaItems.EnsureSourceAsync(
                        publication,
                        "rss",
                        url,
                        rssUrl: url,
                        siteUrl: url,
                        ct: cancellationToken);

                    RssFeedFetchResult fetched;
                    try
                    {
                        fetched = await _rss.FetchFeedAsync(
                            url,
                            _options.MaxItemsPerSource,
                            publication,
                            includeFullContent: true,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger.LogWarning(ex, "RSS feed fetch threw for {Name} ({Url}) during {JobId}.", publication, url, JobId);
                        await _tracker.LogErrorAsync(
                            Provider,
                            JobId,
                            "rss_feed",
                            $"RSS fetch threw for {url}: {ex.Message}",
                            run.Id,
                            FeedHost(url),
                            cancellationToken,
                            trackOperationalError: false);
                        continue;
                    }

                    if (fetched.Failure is not null)
                    {
                        failed++;
                        _logger.LogWarning(
                            "RSS feed fetch failed for {Name} ({Url}): {Reason} {Detail}",
                            publication,
                            url,
                            fetched.Failure.Reason,
                            fetched.Failure.Detail ?? fetched.Failure.SafeMessage);
                        await _tracker.LogErrorAsync(
                            Provider,
                            JobId,
                            "rss_feed",
                            $"{fetched.Failure.SafeMessage} reason={fetched.Failure.Reason} url={url}",
                            run.Id,
                            FeedHost(url),
                            cancellationToken,
                            trackOperationalError: false);
                        continue;
                    }

                    var articles = fetched.Items;

                    foreach (var article in articles)
                    {
                        try
                        {
                            var (c, u, _, _) = await _mediaItems.UpsertItemAsync(source, article, cancellationToken);
                            feedCreated += c;
                            feedUpdated += u;
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            _logger.LogWarning(ex, "Failed to upsert RSS item {ExternalId}.", article.ExternalId);
                            await _tracker.LogErrorAsync(
                                Provider,
                                JobId,
                                "media_item",
                                ex.Message,
                                run.Id,
                                article.ExternalId,
                                cancellationToken);
                        }
                    }

                    await _db.SaveChangesAsync(cancellationToken);
                    created += feedCreated;
                    updated += feedUpdated;
                }
                catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pg &&
                                                   pg.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
                {
                    _db.ChangeTracker.Clear();
                    _logger.LogWarning(ex, "Skipped duplicate RSS media rows for {Url}.", url);
                }
                catch (Exception ex)
                {
                    failed++;
                    _db.ChangeTracker.Clear();
                    _logger.LogWarning(ex, "RSS feed sync failed for {Name} ({Url}).", publication, url);
                    await _tracker.LogErrorAsync(
                        Provider,
                        JobId,
                        "rss_feed",
                        $"RSS feed sync failed for {url}: {ex.Message}",
                        run.Id,
                        FeedHost(url),
                        cancellationToken);
                }
            }

            await _tracker.CompleteAsync(run, created, updated, failed, ct: cancellationToken);
            _logger.LogInformation(
                "RSS opinion sync: {Created} created, {Updated} updated, {Failed} failed.",
                created,
                updated,
                failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RSS opinion sync failed.");
            await _tracker.FailAsync(run, created, updated, ex, cancellationToken);
        }
    }

    private async Task RefreshFeedUrlsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _resolver.EnsureUrlsAsync(ct);
            _logger.LogInformation(
                "RSS URL refresh before {JobId}: {Updated} updated, {Failed} failed, {Deactivated} deactivated.",
                JobId,
                result.Updated,
                result.Failed,
                result.Deactivated);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RSS URL refresh before {JobId} failed; continuing with catalog URLs.", JobId);
        }
    }

    private async Task<IReadOnlyList<(string Name, string Url)>> ResolvePunditFeedsAsync(CancellationToken ct)
    {
        var catalog = await _catalog.GetActiveForPunditAsync(ct);
        if (catalog.Count > 0)
        {
            return catalog
                .Where(f => !RssSourcePolicy.IsDisallowed(f.RssUrl))
                .Select(f => (f.Name, Url: f.RssUrl.Trim()))
                .GroupBy(f => f.Url, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        return _options.RssFeedUrls
            .Where(u => !string.IsNullOrWhiteSpace(u) && !RssSourcePolicy.IsDisallowed(u))
            .Select(u => (Name: ResolvePublicationName(u.Trim()), Url: u.Trim()))
            .GroupBy(f => f.Url, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private string ResolvePublicationName(string feedUrl)
    {
        if (_banterConfig.RssFeedSourceNames.TryGetValue(feedUrl.Trim(), out var configured))
        {
            return configured;
        }

        return "RSS Feed";
    }

    private static string FeedHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host)
            ? uri.Host
            : url;
}
