using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Pundits;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Opinions;

public sealed class PunditFollowService(AppDbContext db)
{
    public const int MaxFollowsPerSession = 40;

    public async Task<IReadOnlyList<Guid>> GetFollowedPunditIdsAsync(
        IUserContext user,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return [];
        }

        return await ForUser(db.PunditFollows.AsNoTracking(), user)
            .Select(f => f.PunditId)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountFollowsAsync(IUserContext user, CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return 0;
        }

        return await ForUser(db.PunditFollows.AsNoTracking(), user).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PunditSummaryDto>> ListDirectoryAsync(
        PunditKind kind,
        int take,
        IUserContext user,
        CancellationToken cancellationToken)
    {
        var pundits = await db.Pundits
            .AsNoTracking()
            .Where(p => p.Kind == kind)
            .OrderBy(p => p.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        var ids = pundits.Select(p => p.Id).ToList();
        var followed = new HashSet<Guid>(await GetFollowedPunditIdsAsync(user, cancellationToken));

        var opinionCounts = await db.PunditOpinions
            .AsNoTracking()
            .Where(o => ids.Contains(o.PunditId) &&
                        !o.NeedsHumanReview &&
                        o.ReviewStatus != "rejected")
            .GroupBy(o => o.PunditId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var predictionCounts = await db.PunditPredictions
            .AsNoTracking()
            .Where(p => ids.Contains(p.PunditId) && p.MatchId != null)
            .GroupBy(p => p.PunditId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        return pundits.Select(p =>
        {
            var display = PunditDisplayResolver.Resolve(p);
            opinionCounts.TryGetValue(p.Id, out var opinions);
            predictionCounts.TryGetValue(p.Id, out var predictions);
            return new PunditSummaryDto(
                p.Id,
                display.DisplayName,
                p.Role,
                display.DeskLabel,
                opinions,
                predictions,
                followed.Contains(p.Id),
                display.AttributionNote,
                display.SourceUrl,
                display.SourcePlatform,
                display.Archetype,
                display.ParodyCue,
                display.AvatarSeed,
                display.IsFictionalPersona);
        }).ToList();
    }

    public async Task<(PunditSummaryDto? Dto, string? Error, int Status)> FollowAsync(
        Guid punditId,
        IUserContext user,
        CancellationToken cancellationToken)
    {
        var pundit = await db.Pundits.FirstOrDefaultAsync(p => p.Id == punditId, cancellationToken);
        if (pundit is null)
        {
            return (null, "Pundit not found.", StatusCodes.Status404NotFound);
        }

        if (pundit.Kind != PunditKind.Source)
        {
            return (null, "Follow sourced pundits only — parody desks are not followable.", StatusCodes.Status400BadRequest);
        }

        var existing = await ForUser(db.PunditFollows, user)
            .FirstOrDefaultAsync(f => f.PunditId == punditId, cancellationToken);
        if (existing is not null)
        {
            return (await MapOneAsync(pundit, isFollowed: true, cancellationToken), null, StatusCodes.Status200OK);
        }

        var count = await CountFollowsAsync(user, cancellationToken);
        if (count >= MaxFollowsPerSession)
        {
            return (null, $"You can follow up to {MaxFollowsPerSession} pundits.", StatusCodes.Status409Conflict);
        }

        db.PunditFollows.Add(new PunditFollow
        {
            Id = Guid.NewGuid(),
            UserId = user.IsAuthenticated ? user.UserId : null,
            AnonymousUserId = user.IsAuthenticated ? null : user.AnonymousUserId,
            PunditId = punditId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        return (await MapOneAsync(pundit, isFollowed: true, cancellationToken), null, StatusCodes.Status201Created);
    }

    public async Task UnfollowAsync(Guid punditId, IUserContext user, CancellationToken cancellationToken)
    {
        var existing = await ForUser(db.PunditFollows, user)
            .FirstOrDefaultAsync(f => f.PunditId == punditId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        db.PunditFollows.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<PunditSummaryDto> MapOneAsync(Pundit pundit, bool isFollowed, CancellationToken cancellationToken)
    {
        var display = PunditDisplayResolver.Resolve(pundit);
        var opinions = await db.PunditOpinions.CountAsync(
            o => o.PunditId == pundit.Id && !o.NeedsHumanReview && o.ReviewStatus != "rejected",
            cancellationToken);
        var predictions = await db.PunditPredictions.CountAsync(
            p => p.PunditId == pundit.Id && p.MatchId != null,
            cancellationToken);

        return new PunditSummaryDto(
            pundit.Id,
            display.DisplayName,
            pundit.Role,
            display.DeskLabel,
            opinions,
            predictions,
            isFollowed,
            display.AttributionNote,
            display.SourceUrl,
            display.SourcePlatform,
            display.Archetype,
            display.ParodyCue,
            display.AvatarSeed,
            display.IsFictionalPersona);
    }

    private static IQueryable<PunditFollow> ForUser(IQueryable<PunditFollow> query, IUserContext user)
    {
        if (user.IsAuthenticated)
        {
            return query.Where(f => f.UserId == user.UserId);
        }

        if (user.IsAnonymous)
        {
            return query.Where(f => f.AnonymousUserId == user.AnonymousUserId);
        }

        return query.Where(_ => false);
    }
}
