using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Receipts;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Studio;

public sealed class StudioStoryService(
    ReceiptQueryService receipts,
    AppDbContext db)
{
    private const int SectionLimit = 8;

    public async Task<StudioStoriesResponse> BuildAsync(
        Common.IUserContext user,
        CancellationToken cancellationToken)
    {
        var receiptRows = await receipts.ListForUserAsync(user, cancellationToken);
        var latest = receiptRows
            .Take(SectionLimit)
            .Select(r => ToReceiptCard(ReceiptMapper.Map(r), StudioContentCatalog.KindReceipt))
            .ToList();

        var vsPundits = receiptRows
            .Select(ReceiptMapper.Map)
            .Where(HasPunditStory)
            .Take(SectionLimit)
            .Select(r => ToReceiptCard(r, StudioContentCatalog.KindVsPundit))
            .ToList();

        var trending = await LoadTrendingAsync(cancellationToken);

        var projects = await LoadProjectsAsync(user, cancellationToken);

        return new StudioStoriesResponse(latest, vsPundits, trending, projects);
    }

    private async Task<IReadOnlyList<StudioStoryCard>> LoadTrendingAsync(CancellationToken cancellationToken)
    {
        var items = await db.NewsFeedItems
            .AsNoTracking()
            .OrderByDescending(n => n.QualityScore ?? 0)
            .ThenByDescending(n => n.PublishedAt)
            .Take(SectionLimit)
            .ToListAsync(cancellationToken);

        return items.Select(ToTrendingCard).ToList();
    }

    private async Task<IReadOnlyList<StudioStoryCard>> LoadProjectsAsync(
        Common.IUserContext user,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return [];
        }

        var query = db.GeneratedContents
            .AsNoTracking()
            .Where(g => g.Type == GeneratedContentType.ContentPack);

        query = user.IsAuthenticated
            ? query.Where(g => g.UserId == user.UserId)
            : query.Where(g => g.AnonymousUserId == user.AnonymousUserId);

        var rows = await query
            .OrderByDescending(g => g.CreatedAt)
            .Take(SectionLimit)
            .ToListAsync(cancellationToken);

        return rows.Select(ToProjectCard).Where(c => c is not null).Select(c => c!).ToList();
    }

    private static bool HasPunditStory(ReceiptDto receipt) =>
        receipt.PunditTakes.Count > 0 ||
        receipt.StoryTypes.Any(t =>
            string.Equals(t, ReceiptStoryTypes.BeatPundit, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t, ReceiptStoryTypes.PunditBeatUser, StringComparison.OrdinalIgnoreCase));

    private static StudioStoryCard ToReceiptCard(ReceiptDto receipt, string kind)
    {
        var teamA = receipt.Match?.TeamA;
        var teamB = receipt.Match?.TeamB;
        var fixture = teamA is { Length: > 0 } && teamB is { Length: > 0 }
            ? $"{teamA} vs {teamB}"
            : "Match";
        var scoreline = receipt.HomeScore.HasValue && receipt.AwayScore.HasValue
            ? $"{receipt.HomeScore}–{receipt.AwayScore}"
            : null;
        var summary = receipt.StoryCandidates.FirstOrDefault()?.Summary
            ?? (scoreline is not null ? $"{fixture} {scoreline}" : fixture);
        var tags = new List<string> { receipt.StoryType };
        if (kind == StudioContentCatalog.KindVsPundit)
        {
            tags.Add("vs pundit");
        }

        return new StudioStoryCard(
            kind == StudioContentCatalog.KindVsPundit
                ? $"vs-pundit:{receipt.Id}"
                : $"receipt:{receipt.Id}",
            kind,
            fixture,
            summary,
            tags,
            receipt.SettledAt,
            receipt.Id,
            receipt.MatchId,
            null,
            null,
            scoreline,
            receipt.StoryType);
    }

    private static StudioStoryCard ToTrendingCard(NewsFeedItem item) =>
        new(
            $"trending:{item.Id}",
            StudioContentCatalog.KindTrending,
            string.IsNullOrWhiteSpace(item.Title) ? "Football story" : item.Title,
            string.IsNullOrWhiteSpace(item.Summary) ? (item.Source ?? "Sourced feed") : item.Summary,
            string.IsNullOrWhiteSpace(item.Source) ? ["trending"] : ["trending", item.Source],
            item.PublishedAt,
            null,
            string.IsNullOrWhiteSpace(item.MatchId) ? null : item.MatchId,
            item.Id,
            null,
            null,
            null);

    private static StudioStoryCard? ToProjectCard(GeneratedContent row)
    {
        var pack = StudioPackJson.Deserialize(row.Output);
        var title = pack?.Title ?? "Saved content pack";
        var summary = pack is null
            ? "Saved Studio project"
            : $"{pack.ContentType} · {pack.Tone}";
        var tags = new List<string> { "project" };
        if (pack is not null)
        {
            tags.Add(pack.ContentType);
            tags.Add(pack.Tone);
        }

        return new StudioStoryCard(
            $"project:{row.Id}",
            StudioContentCatalog.KindProject,
            title,
            summary,
            tags,
            row.CreatedAt,
            pack?.ReceiptId,
            pack?.MatchId,
            pack?.FeedItemId,
            row.Id,
            null,
            pack?.ContentType);
    }
}
