using System.Collections.Concurrent;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Process-wide GIF uniqueness ledger for the current gameweek window, backed by Postgres so
/// Hangfire jobs and API requests share the same "already shown" set across restarts.
/// </summary>
public sealed class ReactionGifLedger : IReactionGifLedger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReactionGifLedger> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private string? _windowId;
    private ConcurrentDictionary<string, byte> _usedIds = new(StringComparer.OrdinalIgnoreCase);
    private ConcurrentDictionary<int, string> _seedUrls = new();

    public ReactionGifLedger(IServiceScopeFactory scopeFactory, ILogger<ReactionGifLedger> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        var window = GameweekGifWindow.Current();
        if (string.Equals(_windowId, window.Id, StringComparison.Ordinal))
        {
            return;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (string.Equals(_windowId, window.Id, StringComparison.Ordinal))
            {
                return;
            }

            await LoadWindowAsync(window, cancellationToken);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<string?> GetAssignedUrlAsync(int seed, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _seedUrls.TryGetValue(seed, out var url) ? url : null;
    }

    public async Task<bool> TryClaimAsync(
        int seed,
        string gifId,
        string url,
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        if (!_usedIds.TryAdd(gifId, 0))
        {
            return false;
        }

        _seedUrls[seed] = url;

        try
        {
            var window = GameweekGifWindow.Current();
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ReactionGifUses.Add(new ReactionGifUse
            {
                WindowId = window.Id,
                GifId = StringLimits.Truncate(gifId, StringLimits.ReactionGifId)!,
                Url = StringLimits.Truncate(url, StringLimits.ReactionGifUrl)!,
                Seed = seed,
                UsedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _usedIds.TryRemove(gifId, out _);
            _seedUrls.TryRemove(seed, out _);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to persist reaction GIF claim {GifId}; keeping in-memory claim.", gifId);
            return true;
        }
    }

    private async Task LoadWindowAsync(GameweekGifWindow window, CancellationToken cancellationToken)
    {
        var usedIds = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var seedUrls = new ConcurrentDictionary<int, string>();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stored = await db.ReactionGifUses
            .AsNoTracking()
            .Where(x => x.WindowId == window.Id)
            .Select(x => new { x.GifId, x.Url, x.Seed })
            .ToListAsync(cancellationToken);

        foreach (var row in stored)
        {
            usedIds.TryAdd(row.GifId, 0);
            if (row.Seed is { } seed)
            {
                seedUrls.TryAdd(seed, row.Url);
            }
        }

        var feedUrls = await db.NewsFeedItems
            .AsNoTracking()
            .Where(n =>
                n.ImageUrl != null &&
                n.PublishedAt >= window.StartUtc &&
                n.PublishedAt < window.EndUtc)
            .Select(n => n.ImageUrl!)
            .ToListAsync(cancellationToken);

        foreach (var feedUrl in feedUrls)
        {
            usedIds.TryAdd(ReactionMediaIdentity.FromUrl(feedUrl), 0);
        }

        _usedIds = usedIds;
        _seedUrls = seedUrls;
        _windowId = window.Id;
        _logger.LogInformation(
            "Loaded {Count} reaction visuals already used in window {WindowId}.",
            usedIds.Count,
            window.Id);
    }
}
