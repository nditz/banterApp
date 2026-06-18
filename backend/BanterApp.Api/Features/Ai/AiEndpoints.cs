using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.SportsData;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Ai;

public static class AiEndpoints
{
    private const int AnonymousGenerationLimit = 3;

    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai").WithTags("AI");

        group.MapPost("/analyze", Analyze)
            .WithValidation<AnalyzeRequest>();
        group.MapPost("/banter", GenerateBanter)
            .WithValidation<BanterRequest>();
        group.MapPost("/meme", GenerateMeme)
            .WithValidation<MemeRequest>();
        group.MapPost("/video-script", GenerateVideoScript)
            .WithValidation<VideoScriptRequest>();
        group.MapPost("/broadcast-script", GenerateBroadcastScript)
            .WithValidation<BroadcastScriptRequest>();

        return app;
    }

    private static async Task<IResult> Analyze(
        AnalyzeRequest request,
        AppDbContext db,
        IUserContext user,
        IContentGenerator generator,
        ISportsDataProvider sports,
        CancellationToken ct)
    {
        var (allowed, remaining, error) = await CheckAnonymousLimitAsync(db, user, ct);
        if (!allowed)
        {
            return Results.Problem(error, statusCode: StatusCodes.Status429TooManyRequests);
        }

        var stats = await sports.GetMatchStatisticsAsync(request.MatchId, ct);
        if (stats is null)
        {
            stats = new Integrations.SportsData.Dtos.MatchStatisticsDto(
                request.MatchId, 50, 50, 10, 10, 4, 4, 5, 5, 8, 8, 2, 2, 1, 1);
        }

        var userKey = ResolveUserKey(user);
        var content = await generator.GenerateAnalysisAsync(
            request.UserPrediction, stats, userKey, isAnonymous: false, ct);

        await RecordGenerationAsync(db, user, GeneratedContentType.Analyze, request.UserPrediction, content, ct);

        return Results.Ok(new AiGenerationResponse(content, "analyze", remaining));
    }

    private static async Task<IResult> GenerateBanter(
        BanterRequest request,
        AppDbContext db,
        IUserContext user,
        IContentGenerator generator,
        CancellationToken ct)
    {
        var (allowed, remaining, error) = await CheckAnonymousLimitAsync(db, user, ct);
        if (!allowed)
        {
            return Results.Problem(error, statusCode: StatusCodes.Status429TooManyRequests);
        }

        var userKey = ResolveUserKey(user);
        var content = await generator.GenerateBanterAsync(
            request.UserPrediction, request.ActualResult, request.Tone, userKey, isAnonymous: false, ct);

        await RecordGenerationAsync(db, user, GeneratedContentType.Banter,
            $"{request.UserPrediction}|{request.ActualResult}", content, ct);

        return Results.Ok(new AiGenerationResponse(content, "banter", remaining));
    }

    private static async Task<IResult> GenerateMeme(
        MemeRequest request,
        AppDbContext db,
        IUserContext user,
        IContentGenerator generator,
        CancellationToken ct)
    {
        var (allowed, remaining, error) = await CheckAnonymousLimitAsync(db, user, ct);
        if (!allowed)
        {
            return Results.Problem(error, statusCode: StatusCodes.Status429TooManyRequests);
        }

        var userKey = ResolveUserKey(user);
        var content = await generator.GenerateMemeCaptionAsync(request.Context, userKey, isAnonymous: false, ct);
        var imageUrl = await generator.GenerateReactionImageUrlAsync(
            request.Context,
            content,
            "meme",
            ct);

        await RecordGenerationAsync(db, user, GeneratedContentType.Meme, request.Context, content, ct);

        return Results.Ok(new AiGenerationResponse(content, "meme", remaining, imageUrl));
    }

    private static async Task<IResult> GenerateVideoScript(
        VideoScriptRequest request,
        AppDbContext db,
        IUserContext user,
        IContentGenerator generator,
        CancellationToken ct)
    {
        var (allowed, remaining, error) = await CheckAnonymousLimitAsync(db, user, ct);
        if (!allowed)
        {
            return Results.Problem(error, statusCode: StatusCodes.Status429TooManyRequests);
        }

        var userKey = ResolveUserKey(user);
        var content = await generator.GenerateVideoScriptAsync(
            request.Format, request.Duration, request.Context, userKey, isAnonymous: false, ct);

        await RecordGenerationAsync(db, user, GeneratedContentType.VideoScript, request.Context, content, ct);

        return Results.Ok(new AiGenerationResponse(content, "video-script", remaining));
    }

    private static async Task<IResult> GenerateBroadcastScript(
        BroadcastScriptRequest request,
        AppDbContext db,
        IUserContext user,
        ISportsDataProvider sports,
        CancellationToken ct)
    {
        var (allowed, remaining, error) = await CheckAnonymousLimitAsync(db, user, ct);
        if (!allowed)
        {
            return Results.Problem(error, statusCode: StatusCodes.Status429TooManyRequests);
        }

        // Pull match stats from the sports data provider for every pick we can
        var statsByMatchId = new Dictionary<string, Integrations.SportsData.Dtos.MatchStatisticsDto>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var matchId in request.Picks
            .Where(p => !string.IsNullOrWhiteSpace(p.MatchId))
            .Select(p => p.MatchId!)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var stats = await sports.GetMatchStatisticsAsync(matchId, ct);
            if (stats is not null)
            {
                statsByMatchId[matchId] = stats;
            }
        }

        var content = BroadcastScriptComposer.Compose(request.Phase, request.Style, request.Picks, statsByMatchId);

        await RecordGenerationAsync(db, user, GeneratedContentType.VideoScript,
            $"broadcast:{request.Phase}:{request.Picks.Count} picks", content, ct);

        return Results.Ok(new AiGenerationResponse(content, "broadcast-script", remaining));
    }

    private static async Task<(bool Allowed, int? Remaining, string? Error)> CheckAnonymousLimitAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        if (user.IsAuthenticated)
        {
            return (true, null, null);
        }

        if (!user.AnonymousUserId.HasValue)
        {
            return (false, 0, "Anonymous user context required.");
        }

        var anonymous = await db.AnonymousUsers.FindAsync([user.AnonymousUserId.Value], ct);
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

    private static async Task RecordGenerationAsync(
        AppDbContext db,
        IUserContext user,
        GeneratedContentType type,
        string prompt,
        string output,
        CancellationToken ct)
    {
        if (user.IsAnonymous && user.AnonymousUserId.HasValue)
        {
            var anonymous = await db.AnonymousUsers.FindAsync([user.AnonymousUserId.Value], ct);
            if (anonymous is not null)
            {
                anonymous.AiGenerationsUsed++;
            }
        }

        db.GeneratedContents.Add(new GeneratedContent
        {
            Id = Guid.NewGuid(),
            UserId = user.IsAuthenticated ? user.UserId : null,
            AnonymousUserId = user.IsAnonymous ? user.AnonymousUserId : null,
            Type = type,
            Prompt = prompt,
            Output = output
        });

        await db.SaveChangesAsync(ct);
    }

    private static string? ResolveUserKey(IUserContext user) =>
        user.IsAuthenticated
            ? user.UserId?.ToString()
            : user.AnonymousCookieId ?? user.AnonymousUserId?.ToString();
}
