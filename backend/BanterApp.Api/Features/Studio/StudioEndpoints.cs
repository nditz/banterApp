using BanterApp.Api.Common;

namespace BanterApp.Api.Features.Studio;

public static class StudioEndpoints
{
    public static IEndpointRouteBuilder MapStudioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/studio").WithTags("Studio");
        group.MapGet("/comparison", GetComparison);
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
}
