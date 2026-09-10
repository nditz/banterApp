using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Ai;

public sealed record PromptDefinition(
    string Key,
    string Label,
    string Description,
    bool ApplyCompetitionFocus);

public static class PromptKeys
{
    public const string NewsReaction = "NewsReactionSystemPrompt";
    public const string FeedBanter = "FeedBanterSystemPrompt";
    public const string MemeImage = "MemeImagePrompt";
    public const string FeedVisual = "FeedVisualSystemPrompt";
    public const string PunditExtraction = "PunditExtractionSystemPrompt";

    public static readonly IReadOnlyList<PromptDefinition> All =
    [
        new(
            NewsReaction,
            "News reaction",
            "Voice for short feed reactions to headlines.",
            ApplyCompetitionFocus: true),
        new(
            FeedBanter,
            "Feed banter",
            "JSON rewrite of news and pundit takes for the timeline.",
            ApplyCompetitionFocus: true),
        new(
            MemeImage,
            "Meme image",
            "Prefix for DALL-E stills. Not a chat system prompt.",
            ApplyCompetitionFocus: false),
        new(
            FeedVisual,
            "Feed GIF picker",
            "JSON picker for Giphy search phrases on feed cards.",
            ApplyCompetitionFocus: true),
        new(
            PunditExtraction,
            "Pundit extraction",
            "Structured extraction of real pundit opinions. Do not loosen quote rules.",
            ApplyCompetitionFocus: true)
    ];

    public static bool TryGet(string? key, out PromptDefinition definition)
    {
        definition = All.FirstOrDefault(d =>
            string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase))!;
        return definition is not null;
    }
}

public interface IPromptCatalog
{
    Task<string> ResolveAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromptCatalogEntry>> ListAsync(CancellationToken cancellationToken = default);

    Task<PromptCatalogEntry> SaveAsync(
        string key,
        string? body,
        Guid? updatedByUserId,
        CancellationToken cancellationToken = default);
}

public sealed record PromptCatalogEntry(
    string Key,
    string Label,
    string Description,
    string Body,
    string DefaultBody,
    bool IsOverride,
    bool ApplyCompetitionFocus,
    DateTimeOffset? UpdatedAt);

public sealed class PromptCatalog(
    AppDbContext db,
    IOptions<AiOptions> options,
    IMemoryCache cache) : IPromptCatalog
{
    public const string CacheKeyPrefix = "prompt-override:";
    public static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public async Task<string> ResolveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!PromptKeys.TryGet(key, out var definition))
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown prompt key.");
        }

        var fallback = DefaultBody(definition.Key);
        var overrideBody = await ReadOverrideAsync(definition.Key, cancellationToken);
        var body = string.IsNullOrWhiteSpace(overrideBody) ? fallback : overrideBody;
        return definition.ApplyCompetitionFocus
            ? CompetitionFocus.ApplyToSystemPrompt(body)
            : body;
    }

    public async Task<IReadOnlyList<PromptCatalogEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.PromptOverrides.AsNoTracking().ToListAsync(cancellationToken);
        var byKey = rows.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);

        return PromptKeys.All.Select(definition =>
        {
            byKey.TryGetValue(definition.Key, out var row);
            var defaultBody = DefaultBody(definition.Key);
            var isOverride = row is not null && !string.IsNullOrWhiteSpace(row.Body);
            return new PromptCatalogEntry(
                definition.Key,
                definition.Label,
                definition.Description,
                isOverride ? row!.Body : defaultBody,
                defaultBody,
                isOverride,
                definition.ApplyCompetitionFocus,
                row?.UpdatedAt);
        }).ToList();
    }

    public async Task<PromptCatalogEntry> SaveAsync(
        string key,
        string? body,
        Guid? updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (!PromptKeys.TryGet(key, out var definition))
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown prompt key.");
        }

        var trimmed = body?.Trim();
        var existing = await db.PromptOverrides.FirstOrDefaultAsync(
            p => p.Key == definition.Key,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            if (existing is not null)
            {
                db.PromptOverrides.Remove(existing);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            if (trimmed.Length > PromptOverrideLimits.Body)
            {
                throw new ArgumentException(
                    $"Prompt body exceeds {PromptOverrideLimits.Body} characters.",
                    nameof(body));
            }

            var now = DateTimeOffset.UtcNow;
            if (existing is null)
            {
                db.PromptOverrides.Add(new PromptOverride
                {
                    Key = definition.Key,
                    Body = trimmed,
                    UpdatedAt = now,
                    UpdatedByUserId = updatedByUserId
                });
            }
            else
            {
                existing.Body = trimmed;
                existing.UpdatedAt = now;
                existing.UpdatedByUserId = updatedByUserId;
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        cache.Remove(CacheKeyPrefix + definition.Key);
        var list = await ListAsync(cancellationToken);
        return list.Single(e => e.Key == definition.Key);
    }

    private async Task<string?> ReadOverrideAsync(string key, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(CacheKeyPrefix + key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var row = await db.PromptOverrides.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Key == key, cancellationToken);
            return row?.Body ?? string.Empty;
        });
    }

    private string DefaultBody(string key) => key switch
    {
        PromptKeys.NewsReaction => options.Value.NewsReactionSystemPrompt,
        PromptKeys.FeedBanter => options.Value.FeedBanterSystemPrompt,
        PromptKeys.MemeImage => options.Value.MemeImagePrompt,
        PromptKeys.FeedVisual => options.Value.FeedVisualSystemPrompt,
        PromptKeys.PunditExtraction => options.Value.PunditExtractionSystemPrompt,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown prompt key.")
    };
}
