using BanterApp.Api.Common;
using BanterApp.Api.Data;

namespace BanterApp.Api.Features.Receipts;

public static class ReceiptEndpoints
{
    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/receipts").WithTags("Receipts");
        group.MapGet("/", ListReceipts).RequireRateLimiting(RateLimitPolicies.PublicPredictions);
        group.MapGet("/{id:guid}", GetReceipt).RequireRateLimiting(RateLimitPolicies.PublicPredictions);
        return app;
    }

    private static async Task<IResult> ListReceipts(
        ReceiptQueryService queries,
        IUserContext user,
        HttpContext http,
        AppDbContext db,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var rows = await queries.ListForUserAsync(user, ct);
        return Results.Ok(rows.Select(ReceiptMapper.Map).ToList());
    }

    private static async Task<IResult> GetReceipt(
        Guid id,
        ReceiptQueryService queries,
        IUserContext user,
        HttpContext http,
        AppDbContext db,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var receipt = await queries.GetOwnedAsync(id, user, ct);
        return receipt is null ? Results.NotFound() : Results.Ok(ReceiptMapper.Map(receipt));
    }
}
