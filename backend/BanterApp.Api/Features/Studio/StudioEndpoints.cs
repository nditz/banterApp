using BanterApp.Api.Common;

namespace BanterApp.Api.Features.Studio;

public static class StudioEndpoints
{
    public static IEndpointRouteBuilder MapStudioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/studio").WithTags("Studio");
        group.MapGet("/comparison", GetComparison);
        group.MapGet("/stories", GetStories).RequireRateLimiting(RateLimitPolicies.PublicPredictions);
        group.MapPost("/packs", CreatePack)
            .RequireRateLimiting(RateLimitPolicies.Write)
            .WithValidation<CreateStudioPackRequest>();
        group.MapGet("/packs", ListPacks).RequireRateLimiting(RateLimitPolicies.PublicPredictions);
        group.MapGet("/packs/{id:guid}", GetPack).RequireRateLimiting(RateLimitPolicies.PublicPredictions);
        return app;
    }

    private static async Task<IResult> GetComparison(
        StudioComparisonService comparison,
        IUserContext user,
        string? matchIds,
        CancellationToken ct)
    {
        var result = await comparison.BuildAsync(user, matchIds, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetStories(
        StudioStoryService stories,
        IUserContext user,
        CancellationToken ct)
    {
        var result = await stories.BuildAsync(user, ct);
        return Results.Ok(result);
    }

    private static Task<IResult> CreatePack(
        CreateStudioPackRequest request,
        StudioPackService packs,
        IUserContext user,
        HttpContext http,
        CancellationToken ct) =>
        packs.GenerateAsync(request, user, http, ct);

    private static Task<IResult> ListPacks(
        StudioPackService packs,
        IUserContext user,
        HttpContext http,
        CancellationToken ct) =>
        packs.ListAsync(user, http, ct);

    private static Task<IResult> GetPack(
        Guid id,
        StudioPackService packs,
        IUserContext user,
        HttpContext http,
        CancellationToken ct) =>
        packs.GetAsync(id, user, http, ct);
}
