using BanterApp.Api.Data;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class YouTubeSearchSyncJob
{
    public const string JobId = "youtube-opinion-sync";
    private const string Provider = "pundit-youtube";

    private readonly AppDbContext _db;
    private readonly IYouTubeProvider _youtube;
    private readonly PunditMediaItemService _mediaItems;
    private readonly PunditIngestOptions _options;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<YouTubeSearchSyncJob> _logger;

    public YouTubeSearchSyncJob(
        AppDbContext db,
        IYouTubeProvider youtube,
        PunditMediaItemService mediaItems,
        IOptions<PunditIngestOptions> options,
        SyncRunTracker tracker,
        ILogger<YouTubeSearchSyncJob> logger)
    {
        _db = db;
        _youtube = youtube;
        _mediaItems = mediaItems;
        _options = options.Value;
        _tracker = tracker;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_youtube.IsConfigured)
        {
            return;
        }

        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var created = 0;
        var updated = 0;
        var failed = 0;

        try
        {
            var source = await _mediaItems.EnsureSourceAsync(
                "YouTube Search",
                "youtube",
                "youtube-search",
                siteUrl: "https://www.youtube.com",
                configJson: null,
                ct: cancellationToken);

            foreach (var query in _options.YouTubeSearchQueries.Where(q => !string.IsNullOrWhiteSpace(q)))
            {
                var videos = await _youtube.SearchVideosAsync(
                    query.Trim(),
                    _options.MaxItemsPerSource,
                    cancellationToken);

                foreach (var video in videos)
                {
                    try
                    {
                        var (c, u, skipped, _) = await _mediaItems.UpsertItemAsync(source, video, cancellationToken);
                        created += c;
                        updated += u;
                        if (skipped > 0 && c == 0 && u == 0)
                        {
                            // not a failure
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _logger.LogWarning(ex, "Failed to upsert YouTube video {ExternalId}.", video.ExternalId);
                        await _tracker.LogErrorAsync(
                            Provider,
                            JobId,
                            "media_item",
                            ex.Message,
                            run.Id,
                            video.ExternalId,
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
                "YouTube opinion sync: {Created} created, {Updated} updated, {Failed} failed.",
                created,
                updated,
                failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube opinion sync failed.");
            await _tracker.FailAsync(run, created, updated, ex, cancellationToken);
        }
    }
}
