using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Media;

public sealed record GifAssetSeed(string Url, string Title, string Description, string Mood, string Tags);

/// <summary>
/// First-party GIF/sticker library plus football search phrases. Assets are bundled or
/// licensed files we own; queries are text used later for throttled live lookups.
/// </summary>
public sealed class GifLibraryService
{
    public static readonly IReadOnlyList<GifAssetSeed> BundledSeeds =
    [
        new(
            "/reactions/receipts-found.svg",
            "Receipts found",
            "Celebration when a bold prediction lands. I-told-you-so energy after the result drops.",
            "celebrate",
            "celebrate,win,trophy,hype"),
        new(
            "/reactions/locked-in.svg",
            "Locked in",
            "Hype and focus before a big match. Main-character energy on the timeline.",
            "hype",
            "hype,maincharacter,cooked"),
        new(
            "/reactions/against-grain.svg",
            "Against the grain",
            "Contrarian take, group-chat debate, and spicy disagreement with the consensus.",
            "debate",
            "debate,pundit,delulu,ratio"),
        new(
            "/reactions/chaos-pick.svg",
            "Chaos pick",
            "Shock result, chaos scoreline, or a pick nobody saw coming.",
            "shock",
            "shock,chaos,delulu"),
        new(
            "/reactions/prediction-fraud.svg",
            "Prediction fraud",
            "Facepalm after a cooked prediction. The call aged like milk.",
            "facepalm",
            "facepalm,miss,roast,cooked"),
        new(
            "/reactions/brave-but-wrong.svg",
            "Brave but wrong",
            "Roast energy for a confident take that missed. Brave, still wrong.",
            "roast",
            "roast,facepalm,miss,ratio"),
        new(
            "/reactions/script-writer.svg",
            "Script writer",
            "Trophy lift, movie-script ending, last-minute winner energy.",
            "trophy",
            "trophy,celebrate,win"),
        new(
            "/reactions/smart-choice.svg",
            "Smart choice",
            "Breaking news reaction and the sensible, receipts-backed take.",
            "news",
            "news,pundit"),
        new(
            "/reactions/playing-safe.svg",
            "Playing safe",
            "Pundit-desk energy. Safe pick, studio consensus, playing it down the middle.",
            "pundit",
            "pundit,news,debate"),
    ];

    private readonly AppDbContext _db;

    public GifLibraryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        await EnsureBundledAssetsAsync(cancellationToken);
        await EnsureCuratedQueriesAsync(cancellationToken);
    }

    public async Task<GifAsset?> PickAsync(
        string? mood,
        IEnumerable<string?>? queries,
        Func<string, string, Task<bool>> tryClaimAsync,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);

        var assets = await _db.GifAssets
            .AsNoTracking()
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);

        if (assets.Count == 0)
        {
            return null;
        }

        var ranked = assets
            .Select(asset => (Asset: asset, Score: Score(asset, mood, queries)))
            .OrderByDescending(x => x.Score)
            .ThenBy(_ => Random.Shared.Next())
            .Select(x => x.Asset)
            .ToList();

        foreach (var asset in ranked)
        {
            if (await tryClaimAsync(ReactionMediaIdentity.FromUrl(asset.Url), asset.Url))
            {
                return asset;
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<string>> GetActiveQueriesAsync(
        int take = 8,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);

        var phrases = await _db.GifSearchQueries
            .AsNoTracking()
            .Where(q => q.IsActive && q.IsFootballRelated)
            .OrderByDescending(q => q.LastSeenAtUtc)
            .Select(q => q.Phrase)
            .Take(Math.Clamp(take, 1, 24))
            .ToListAsync(cancellationToken);

        GiphyGifSelector.Shuffle(phrases);
        return phrases;
    }

    public async Task UpsertQueriesAsync(
        IEnumerable<string> phrases,
        string source,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = await _db.GifSearchQueries.ToListAsync(cancellationToken);
        var byPhrase = existing.ToDictionary(q => q.Phrase, StringComparer.OrdinalIgnoreCase);

        foreach (var raw in phrases)
        {
            var phrase = StringLimits.Truncate(raw.Trim(), GifSearchQueryLimits.Phrase);
            if (string.IsNullOrWhiteSpace(phrase))
            {
                continue;
            }

            var football = FootballGifQuery.LooksFootball(phrase)
                || string.Equals(source, GifSearchQuerySources.Curated, StringComparison.OrdinalIgnoreCase);

            if (byPhrase.TryGetValue(phrase, out var row))
            {
                row.LastSeenAtUtc = now;
                row.IsFootballRelated = row.IsFootballRelated || football;
                row.IsActive = true;
                if (!string.Equals(row.Source, GifSearchQuerySources.Curated, StringComparison.OrdinalIgnoreCase))
                {
                    row.Source = StringLimits.Truncate(source, GifSearchQueryLimits.Source) ?? source;
                }
            }
            else
            {
                var added = new GifSearchQuery
                {
                    Id = Guid.NewGuid(),
                    Phrase = phrase,
                    Source = StringLimits.Truncate(source, GifSearchQueryLimits.Source) ?? source,
                    IsFootballRelated = football,
                    IsActive = football,
                    LastSeenAtUtc = now,
                    CreatedAtUtc = now,
                };
                _db.GifSearchQueries.Add(added);
                byPhrase[phrase] = added;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBundledAssetsAsync(CancellationToken cancellationToken)
    {
        var existingUrls = await _db.GifAssets
            .Select(a => a.Url)
            .ToListAsync(cancellationToken);
        var known = existingUrls.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var added = 0;

        foreach (var seed in BundledSeeds)
        {
            if (!known.Add(seed.Url))
            {
                continue;
            }

            _db.GifAssets.Add(new GifAsset
            {
                Id = Guid.NewGuid(),
                Url = seed.Url,
                Title = seed.Title,
                Description = seed.Description,
                Mood = seed.Mood,
                Tags = seed.Tags,
                Source = GifAssetSources.Bundled,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureCuratedQueriesAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.GifSearchQueries
            .Select(q => q.Phrase)
            .ToListAsync(cancellationToken);
        var known = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (FootballGifQuery.CuratedPhrases.All(known.Contains))
        {
            return;
        }

        await UpsertQueriesAsync(FootballGifQuery.CuratedPhrases, GifSearchQuerySources.Curated, cancellationToken);
    }

    private static int Score(GifAsset asset, string? mood, IEnumerable<string?>? queries)
    {
        var score = 1;
        var haystack = $"{asset.Mood} {asset.Tags} {asset.Title} {asset.Description}";

        if (!string.IsNullOrWhiteSpace(mood) &&
            haystack.Contains(mood.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            score += 8;
        }

        if (queries is null)
        {
            return score;
        }

        foreach (var raw in queries)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            foreach (var token in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (token.Length < 4)
                {
                    continue;
                }

                if (haystack.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    score += 2;
                }
            }
        }

        return score;
    }
}
