using BanterApp.Api.Common;

namespace BanterApp.Api.Features.Privacy;

public static class ConsentEndpoints
{
    public static void MapConsentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/consent").WithTags("Privacy");

        group.MapGet("/", async (IConsentService consent, IUserContext user, CancellationToken ct) =>
            Results.Ok(await consent.GetAsync(user, ct)));

        group.MapPost("/", SaveConsent)
            .RequireRateLimiting(RateLimitPolicies.ConsentUpdate);
    }

    private static async Task<IResult> SaveConsent(
        ConsentUpdateRequest request,
        IConsentService consent,
        IUserContext user,
        CancellationToken ct)
    {
        // "necessary" is implicit and cannot be declined; it is not accepted as input so
        // a client cannot imply it was optional.
        var state = await consent.SaveAsync(
            user,
            request.Analytics,
            request.Marketing,
            ct);

        return Results.Ok(state);
    }
}

public sealed record ConsentUpdateRequest(bool Analytics, bool Marketing);
