using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Receipts;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Studio;

public sealed class StudioPackService(
    AppDbContext db,
    ReceiptQueryService receipts)
{
    private const int AnonymousGenerationLimit = 3;

    public async Task<IResult> GenerateAsync(
        CreateStudioPackRequest request,
        IUserContext user,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, cancellationToken);
        if (guard is not null)
        {
            return guard;
        }

        var (allowed, remaining, error) = await CheckAnonymousLimitAsync(user, cancellationToken);
        if (!allowed)
        {
            return Results.Problem(error, statusCode: StatusCodes.Status429TooManyRequests);
        }

        var assembled = await AssembleAsync(request, user, cancellationToken);
        if (assembled.Error is not null)
        {
            return assembled.Error;
        }

        var packId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var pack = StudioContentPackComposer.Compose(
            assembled.Context!,
            request.ContentType,
            request.Tone,
            packId,
            createdAt);

        await PersistAsync(user, pack, cancellationToken);

        return Results.Ok(new StudioPackGenerateResponse(pack, remaining));
    }

    public async Task<IResult> ListAsync(
        IUserContext user,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, cancellationToken);
        if (guard is not null)
        {
            return guard;
        }

        var rows = await OwnedPacks(user)
            .OrderByDescending(g => g.CreatedAt)
            .Take(30)
            .ToListAsync(cancellationToken);

        var packs = rows
            .Select(r => StudioPackJson.Deserialize(r.Output))
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        return Results.Ok(packs);
    }

    public async Task<IResult> GetAsync(
        Guid id,
        IUserContext user,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, cancellationToken);
        if (guard is not null)
        {
            return guard;
        }

        var row = await OwnedPacks(user)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (row is null)
        {
            return Results.NotFound();
        }

        var pack = StudioPackJson.Deserialize(row.Output);
        return pack is null ? Results.NotFound() : Results.Ok(pack);
    }

    private async Task<(StudioPackContext? Context, IResult? Error)> AssembleAsync(
        CreateStudioPackRequest request,
        IUserContext user,
        CancellationToken cancellationToken)
    {
        if (request.ReceiptId is { } receiptId)
        {
            var receipt = await receipts.GetOwnedAsync(receiptId, user, cancellationToken);
            if (receipt is null)
            {
                return (null, Results.NotFound());
            }

            return (StudioContextAssembler.FromReceipt(ReceiptMapper.Map(receipt)), null);
        }

        if (!string.IsNullOrWhiteSpace(request.FeedItemId))
        {
            var item = await db.NewsFeedItems
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == request.FeedItemId, cancellationToken);
            if (item is null)
            {
                return (null, Results.NotFound());
            }

            return (StudioContextAssembler.FromNews(item), null);
        }

        if (request.ProjectId is { } projectId)
        {
            var row = await OwnedPacks(user)
                .FirstOrDefaultAsync(g => g.Id == projectId, cancellationToken);
            if (row is null)
            {
                return (null, Results.NotFound());
            }

            var previous = StudioPackJson.Deserialize(row.Output);
            if (previous is null)
            {
                return (null, Results.NotFound());
            }

            if (previous.ReceiptId is { } linkedReceipt)
            {
                var receipt = await receipts.GetOwnedAsync(linkedReceipt, user, cancellationToken);
                if (receipt is not null)
                {
                    return (StudioContextAssembler.FromReceipt(ReceiptMapper.Map(receipt)), null);
                }
            }

            if (!string.IsNullOrWhiteSpace(previous.FeedItemId))
            {
                var item = await db.NewsFeedItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == previous.FeedItemId, cancellationToken);
                if (item is not null)
                {
                    return (StudioContextAssembler.FromNews(item), null);
                }
            }

            return (StudioContextAssembler.FromPreviousPack(previous), null);
        }

        return (null, Results.BadRequest(new { error = "Select a receipt, trending story, or saved project." }));
    }

    private async Task PersistAsync(IUserContext user, StudioContentPack pack, CancellationToken cancellationToken)
    {
        if (user.IsAnonymous && user.AnonymousUserId.HasValue)
        {
            var anonymous = await db.AnonymousUsers.FindAsync([user.AnonymousUserId.Value], cancellationToken);
            if (anonymous is not null)
            {
                anonymous.AiGenerationsUsed++;
            }
        }

        db.GeneratedContents.Add(new GeneratedContent
        {
            Id = pack.Id,
            UserId = user.IsAuthenticated ? user.UserId : null,
            AnonymousUserId = user.IsAnonymous ? user.AnonymousUserId : null,
            Type = GeneratedContentType.ContentPack,
            Prompt = $"pack:{pack.ContentType}:{pack.Tone}:{pack.StoryKind}:{pack.ReceiptId?.ToString() ?? pack.FeedItemId}",
            Output = StudioPackJson.Serialize(pack),
            CreatedAt = pack.CreatedAt
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<GeneratedContent> OwnedPacks(IUserContext user)
    {
        var query = db.GeneratedContents
            .AsNoTracking()
            .Where(g => g.Type == GeneratedContentType.ContentPack);

        return user.IsAuthenticated
            ? query.Where(g => g.UserId == user.UserId)
            : query.Where(g => g.AnonymousUserId == user.AnonymousUserId);
    }

    private async Task<(bool Allowed, int? Remaining, string? Error)> CheckAnonymousLimitAsync(
        IUserContext user,
        CancellationToken cancellationToken)
    {
        if (user.IsAuthenticated)
        {
            return (true, null, null);
        }

        if (!user.AnonymousUserId.HasValue)
        {
            return (false, 0, "Anonymous user context required.");
        }

        var anonymous = await db.AnonymousUsers.FindAsync([user.AnonymousUserId.Value], cancellationToken);
        if (anonymous is null)
        {
            return (false, 0, "Anonymous user not found.");
        }

        if (anonymous.AiGenerationsUsed >= AnonymousGenerationLimit)
        {
            return (false, 0,
                $"Anonymous users are limited to {AnonymousGenerationLimit} AI content generations. Register for unlimited access.");
        }

        var remaining = AnonymousGenerationLimit - anonymous.AiGenerationsUsed - 1;
        return (true, Math.Max(0, remaining), null);
    }
}
