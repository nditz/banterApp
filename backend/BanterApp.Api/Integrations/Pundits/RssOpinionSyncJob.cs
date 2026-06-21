using BanterApp.Api.Data;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.FootballBanter;
using BanterApp.Api.Integrations.Media;
using Hangfire;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class RssOpinionSyncJob
{
    public const string JobId = "rss-opinion-sync";
    private const string Provider = "pundit-rss";

    private readonly AppDbContext _db;
    private readonly IRssFeedProvider _rss;
    private readonly PunditMediaItemService _mediaItems;
    private readonly PunditIngestOptions _options;
    private readonly IFootballBanterConfigProvider _banterConfig;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<RssOpinionSyncJob> _logger;

    public RssOpinionSyncJob(
        AppDbContext db,
        IRssFeedProvider rss,
        PunditMediaItemService mediaItems,
        IOptions<PunditIngestOptions> options,
        IFootballBanterConfigProvider banterConfig,
        SyncRunTracker tracker,
        ILogger<RssOpinionSyncJob> logger)
    {
        _db = db;
        _rss = rss;
        _mediaItems = mediaItems;
        _options = options.Value;
        _banterConfig = banterConfig;
        _tracker = tracker;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
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
            foreach (var feedUrl in _options.RssFeedUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                var url = feedUrl.Trim();
                var publication = ResolvePublicationName(url);
                var source = await _mediaItems.EnsureSourceAsync(
                    publication,
                    "rss",
                    url,
                    rssUrl: url,
                    siteUrl: url,
                    ct: cancellationToken);

                IReadOnlyList<Media.Dtos.MediaItemDto> articles;
                try
                {
                    articles = await _rss.FetchFeedAsync(
                        url,
                        _options.MaxItemsPerSource,
                        publication,
                        includeFullContent: true,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(ex, "RSS feed fetch failed for {Url}.", url);
                    await _tracker.LogErrorAsync(Provider, JobId, "rss_feed", ex.Message, run.Id, url, cancellationToken);
                    continue;
                }

                foreach (var article in articles)
                {
                    try
                    {
                        var (c, u, _, _) = await _mediaItems.UpsertItemAsync(source, article, cancellationToken);
                        created += c;
                        updated += u;
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
            }

            if (created > 0 || updated > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
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

    private string ResolvePublicationName(string feedUrl)
    {
        if (_banterConfig.RssFeedSourceNames.TryGetValue(feedUrl.Trim(), out var configured))
        {
            return configured;
        }

        return "RSS Feed";
    }
}
