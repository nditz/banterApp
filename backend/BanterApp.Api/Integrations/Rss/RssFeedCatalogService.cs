using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Rss;

public interface IRssFeedCatalog
{
    Task SeedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RssFeed>> GetActiveForMediaIngestAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RssFeed>> GetActiveForNewsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RssFeed>> GetActiveForPunditAsync(CancellationToken ct = default);
}

public sealed class RssFeedCatalogService(AppDbContext db, IRssFeedCatalogSeed seed) : IRssFeedCatalog
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var usedSlugs = (await db.RssFeeds.Select(f => f.Slug).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in seed.Feeds.Where(f => !string.IsNullOrWhiteSpace(f.RssUrl)))
        {
            var kind = string.Equals(entry.Kind, RssFeedKind.Podcast, StringComparison.OrdinalIgnoreCase)
                ? RssFeedKind.Podcast
                : RssFeedKind.Website;
            var name = string.IsNullOrWhiteSpace(entry.Name) ? entry.RssUrl.Trim() : entry.Name.Trim();
            await UpsertAsync(
                new CatalogSeed(
                    RssFeedSlug.From(kind, name),
                    name,
                    kind,
                    entry.RssUrl.Trim(),
                    entry.ApplePodcastId,
                    entry.SiteUrl?.Trim(),
                    entry.StyleSlug?.Trim(),
                    PriorityFromWeight(entry.SourceWeight),
                    entry.ExtractPredictions,
                    entry.UseForMediaIngest,
                    entry.UseForNews,
                    entry.UseForPundit),
                usedSlugs,
                ct);
        }

        await db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<RssFeed>> GetActiveForMediaIngestAsync(CancellationToken ct = default) =>
        QueryActiveAsync(f => f.UseForMediaIngest, ct);

    public Task<IReadOnlyList<RssFeed>> GetActiveForNewsAsync(CancellationToken ct = default) =>
        QueryActiveAsync(f => f.UseForNews, ct);

    public Task<IReadOnlyList<RssFeed>> GetActiveForPunditAsync(CancellationToken ct = default) =>
        QueryActiveAsync(f => f.UseForPundit, ct);

    private async Task<IReadOnlyList<RssFeed>> QueryActiveAsync(
        System.Linq.Expressions.Expression<Func<RssFeed, bool>> channel,
        CancellationToken ct)
    {
        return await db.RssFeeds.AsNoTracking()
            .Where(f => f.IsActive && f.RssUrl != "")
            .Where(channel)
            .OrderByDescending(f => f.Priority)
            .ThenBy(f => f.Name)
            .ToListAsync(ct);
    }

    private async Task UpsertAsync(CatalogSeed catalogSeed, HashSet<string> usedSlugs, CancellationToken ct)
    {
        var existing = await FindExistingAsync(catalogSeed, ct);
        if (existing is not null)
        {
            existing.Name = StringLimits.Truncate(catalogSeed.Name, 120) ?? existing.Name;
            existing.Kind = catalogSeed.Kind;
            existing.ApplePodcastId = catalogSeed.ApplePodcastId ?? existing.ApplePodcastId;
            existing.SiteUrl = StringLimits.Truncate(catalogSeed.SiteUrl, 512) ?? existing.SiteUrl;
            existing.StyleSlug = StringLimits.Truncate(catalogSeed.StyleSlug, StringLimits.RssFeedStyleSlug)
                ?? existing.StyleSlug;
            existing.Priority = catalogSeed.Priority;
            existing.ExtractPredictions = catalogSeed.ExtractPredictions;
            existing.UseForMediaIngest = catalogSeed.UseForMediaIngest;
            existing.UseForNews = catalogSeed.UseForNews;
            existing.UseForPundit = catalogSeed.UseForPundit;
            existing.UpdatedAt = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(existing.RssUrl))
            {
                existing.RssUrl = StringLimits.Truncate(catalogSeed.RssUrl, 512) ?? catalogSeed.RssUrl;
            }

            return;
        }

        var slug = usedSlugs.Add(catalogSeed.Slug) ? catalogSeed.Slug : UniqueSlug(catalogSeed.Slug, usedSlugs);
        db.RssFeeds.Add(new RssFeed
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = StringLimits.Truncate(catalogSeed.Name, 120) ?? catalogSeed.Name,
            Kind = catalogSeed.Kind,
            RssUrl = StringLimits.Truncate(catalogSeed.RssUrl, 512) ?? catalogSeed.RssUrl,
            ApplePodcastId = catalogSeed.ApplePodcastId,
            SiteUrl = StringLimits.Truncate(catalogSeed.SiteUrl, 512),
            StyleSlug = StringLimits.Truncate(catalogSeed.StyleSlug, StringLimits.RssFeedStyleSlug),
            Priority = catalogSeed.Priority,
            ExtractPredictions = catalogSeed.ExtractPredictions,
            UseForMediaIngest = catalogSeed.UseForMediaIngest,
            UseForNews = catalogSeed.UseForNews,
            UseForPundit = catalogSeed.UseForPundit,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private async Task<RssFeed?> FindExistingAsync(CatalogSeed catalogSeed, CancellationToken ct)
    {
        if (catalogSeed.ApplePodcastId is not null)
        {
            var byApple = await db.RssFeeds.FirstOrDefaultAsync(
                f => f.ApplePodcastId == catalogSeed.ApplePodcastId,
                ct);
            if (byApple is not null)
            {
                return byApple;
            }

            byApple = db.RssFeeds.Local.FirstOrDefault(f => f.ApplePodcastId == catalogSeed.ApplePodcastId);
            if (byApple is not null)
            {
                return byApple;
            }
        }

        var bySlug = await db.RssFeeds.FirstOrDefaultAsync(f => f.Slug == catalogSeed.Slug, ct)
            ?? db.RssFeeds.Local.FirstOrDefault(f => f.Slug == catalogSeed.Slug);
        if (bySlug is not null)
        {
            return bySlug;
        }

        return await db.RssFeeds.FirstOrDefaultAsync(f => f.RssUrl == catalogSeed.RssUrl, ct)
            ?? db.RssFeeds.Local.FirstOrDefault(f => RssUrlNormalizer.EqualsUrl(f.RssUrl, catalogSeed.RssUrl));
    }

    private static int PriorityFromWeight(double sourceWeight) =>
        (int)Math.Clamp(Math.Round(sourceWeight * 100), 0, 10_000);

    private static string UniqueSlug(string slug, HashSet<string> usedSlugs)
    {
        for (var i = 2; i < 50; i++)
        {
            var suffix = $"-{i}";
            var maxBase = StringLimits.RssFeedSlug - suffix.Length;
            var candidate = slug.Length <= maxBase ? $"{slug}{suffix}" : $"{slug[..maxBase]}{suffix}";
            if (usedSlugs.Add(candidate))
            {
                return candidate;
            }
        }

        var fallback = Guid.NewGuid().ToString("N")[..StringLimits.RssFeedSlug];
        usedSlugs.Add(fallback);
        return fallback;
    }

    private sealed record CatalogSeed(
        string Slug,
        string Name,
        string Kind,
        string RssUrl,
        long? ApplePodcastId,
        string? SiteUrl,
        string? StyleSlug,
        int Priority,
        bool ExtractPredictions,
        bool UseForMediaIngest,
        bool UseForNews,
        bool UseForPundit);
}
