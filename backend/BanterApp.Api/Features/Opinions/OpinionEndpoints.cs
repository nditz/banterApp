using BanterApp.Api.Common;
using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Opinions;

public static class OpinionEndpoints
{
    public static IEndpointRouteBuilder MapOpinionEndpoints(this IEndpointRouteBuilder app)
    {
        var sources = app.MapGroup("/api/sources").WithTags("Sources").AllowAnonymous();
        sources.MapGet("/", ListSources).RequireRateLimiting(RateLimitPolicies.PublicSearch);

        app.MapGet("/api/source-items/{id:guid}", GetSourceItem)
            .WithTags("Sources")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.PublicArticle);

        var pundits = app.MapGroup("/api/pundits").WithTags("Pundits").AllowAnonymous();
        pundits.MapGet("/", ListPundits).RequireRateLimiting(RateLimitPolicies.PublicSearch);
        pundits.MapGet("/{id:guid}/opinions", GetPunditOpinions).RequireRateLimiting(RateLimitPolicies.PublicSearch);

        var opinions = app.MapGroup("/api/opinions").WithTags("Opinions").AllowAnonymous();
        opinions.MapGet("/", ListOpinions).RequireRateLimiting(RateLimitPolicies.PublicSearch);

        var predictions = app.MapGroup("/api/predictions").WithTags("Predictions").AllowAnonymous();
        predictions.MapGet("/pundits", ListPunditPredictions).RequireRateLimiting(RateLimitPolicies.PublicPredictions);

        return app;
    }

    private static async Task<IResult> ListSources(OpinionQueryService queries, CancellationToken ct) =>
        Results.Ok(await queries.QuerySourcesAsync(ct));

    private static async Task<IResult> GetSourceItem(Guid id, OpinionQueryService queries, CancellationToken ct)
    {
        var item = await queries.GetSourceItemAsync(id, ct);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> ListPundits(
        OpinionQueryService queries,
        string? kind,
        int? pageSize,
        CancellationToken ct)
    {
        var punditKind = string.Equals(kind, "persona", StringComparison.OrdinalIgnoreCase)
            ? PunditKind.Persona
            : PunditKind.Source;
        var take = Math.Clamp(pageSize ?? 50, 1, 100);
        return Results.Ok(await queries.QueryPunditsAsync(punditKind, take, ct));
    }

    private static async Task<IResult> GetPunditOpinions(
        Guid id,
        OpinionQueryService queries,
        int? pageSize,
        CancellationToken ct)
    {
        var take = Math.Clamp(pageSize ?? 50, 1, 100);
        return Results.Ok(await queries.GetPunditOpinionsAsync(id, take, ct));
    }

    private static async Task<IResult> ListOpinions(
        OpinionQueryService queries,
        string? team,
        string? source,
        string? player,
        bool? needsReview,
        DateTimeOffset? publishedAfter,
        int? pageSize,
        CancellationToken ct)
    {
        var take = Math.Clamp(pageSize ?? 50, 1, 100);
        var results = await queries.QueryOpinionsAsync(
            team,
            source,
            player,
            needsReview,
            publishedAfter,
            take,
            ct);
        return Results.Ok(results);
    }

    private static async Task<IResult> ListPunditPredictions(
        OpinionQueryService queries,
        string? team,
        string? entityType,
        int? pageSize,
        CancellationToken ct)
    {
        var take = Math.Clamp(pageSize ?? 50, 1, 100);
        return Results.Ok(await queries.QueryPredictionAggregatesAsync(team, entityType, take, ct));
    }
}
