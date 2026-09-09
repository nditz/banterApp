using BanterApp.Api.Common;
using BanterApp.Api.Data;
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
        pundits.MapGet("/follows", ListFollows).RequireRateLimiting(RateLimitPolicies.PublicSearch);
        pundits.MapPost("/{id:guid}/follow", FollowPundit).RequireRateLimiting(RateLimitPolicies.Write);
        pundits.MapDelete("/{id:guid}/follow", UnfollowPundit).RequireRateLimiting(RateLimitPolicies.Write);
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
        PunditFollowService follows,
        IUserContext user,
        string? kind,
        int? pageSize,
        CancellationToken ct)
    {
        var punditKind = string.Equals(kind, "persona", StringComparison.OrdinalIgnoreCase)
            ? PunditKind.Persona
            : PunditKind.Source;
        var take = Math.Clamp(pageSize ?? 50, 1, 100);
        return Results.Ok(await follows.ListDirectoryAsync(punditKind, take, user, ct));
    }

    private static async Task<IResult> ListFollows(
        PunditFollowService follows,
        IUserContext user,
        CancellationToken ct)
    {
        var directory = await follows.ListDirectoryAsync(PunditKind.Source, 100, user, ct);
        return Results.Ok(directory.Where(p => p.IsFollowed).ToList());
    }

    private static async Task<IResult> FollowPundit(
        Guid id,
        PunditFollowService follows,
        IUserContext user,
        AppDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var (dto, error, status) = await follows.FollowAsync(id, user, ct);
        if (error is not null)
        {
            return Results.Json(new { error }, statusCode: status);
        }

        return status == StatusCodes.Status201Created
            ? Results.Created($"/api/pundits/{id}/follow", dto)
            : Results.Ok(dto);
    }

    private static async Task<IResult> UnfollowPundit(
        Guid id,
        PunditFollowService follows,
        IUserContext user,
        AppDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        await follows.UnfollowAsync(id, user, ct);
        return Results.NoContent();
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
