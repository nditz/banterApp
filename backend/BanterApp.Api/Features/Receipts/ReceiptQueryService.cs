using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Receipts;

public sealed class ReceiptQueryService(AppDbContext db)
{
    public async Task<IReadOnlyList<PredictionReceipt>> ListForUserAsync(
        IUserContext user,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return [];
        }

        var query = db.PredictionReceipts
            .AsNoTracking()
            .Include(r => r.Match)
            .Include(r => r.StoryCandidates)
            .AsQueryable();

        query = user.IsAuthenticated
            ? query.Where(r => r.UserId == user.UserId)
            : query.Where(r => r.AnonymousUserId == user.AnonymousUserId);

        var rows = await query
            .OrderByDescending(r => r.SettledAt)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.PredictionId)
            .Select(g => g.First())
            .OrderByDescending(r => r.SettledAt)
            .ToList();
    }

    public async Task<PredictionReceipt?> GetOwnedAsync(
        Guid id,
        IUserContext user,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return null;
        }

        var receipt = await db.PredictionReceipts
            .AsNoTracking()
            .Include(r => r.Match)
            .Include(r => r.StoryCandidates)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (receipt is null)
        {
            return null;
        }

        var owns = user.IsAuthenticated
            ? receipt.UserId == user.UserId
            : receipt.AnonymousUserId == user.AnonymousUserId;
        return owns ? receipt : null;
    }
}
