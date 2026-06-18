using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Predictions;

public static class PredictionEndpoints
{
    public static IEndpointRouteBuilder MapPredictionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/predictions").WithTags("Predictions");

        group.MapPost("/create", CreatePrediction)
            .RequireRateLimiting("write")
            .WithValidation<CreatePredictionRequest>();
        group.MapPut("/update", UpdatePrediction)
            .RequireRateLimiting("write")
            .WithValidation<UpdatePredictionRequest>();
        group.MapGet("/history", GetPredictionHistory);

        return app;
    }

    private static async Task<IResult> CreatePrediction(
        CreatePredictionRequest request,
        AppDbContext db,
        IUserContext user,
        ScoringService scoring,
        HttpContext http,
        TurnstileService turnstile,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            return Results.BadRequest(new { error = "Human verification failed." });
        }

        var match = await db.Matches.FindAsync([request.MatchId], ct);
        if (match is null)
        {
            return Results.NotFound(new { error = "Match not found." });
        }

        if (MatchLockService.IsLocked(match))
        {
            return Results.BadRequest(new { error = MatchLockService.LockReason(match) });
        }

        var existing = await db.Predictions.FirstOrDefaultAsync(p =>
            p.MatchId == request.MatchId &&
            p.PredictionType == request.PredictionType &&
            (user.IsAuthenticated ? p.UserId == user.UserId : p.AnonymousUserId == user.AnonymousUserId), ct);

        if (existing is not null)
        {
            return Results.Conflict(new { error = "Prediction already exists. Use update endpoint." });
        }

        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = user.IsAuthenticated ? user.UserId : null,
            AnonymousUserId = user.IsAnonymous ? user.AnonymousUserId : null,
            MatchId = request.MatchId,
            PredictionType = request.PredictionType,
            PredictionValue = request.PredictionValue.Trim(),
            PointsAwarded = scoring.CalculatePoints(request.PredictionType, request.PredictionValue, match)
        };

        db.Predictions.Add(prediction);
        await db.SaveChangesAsync(ct);

        return Results.Created("/api/predictions/history", Map(prediction, match));
    }

    private static async Task<IResult> UpdatePrediction(
        UpdatePredictionRequest request,
        AppDbContext db,
        IUserContext user,
        ScoringService scoring,
        HttpContext http,
        TurnstileService turnstile,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            return Results.BadRequest(new { error = "Human verification failed." });
        }

        var prediction = await db.Predictions
            .Include(p => p.Match)
            .FirstOrDefaultAsync(p => p.Id == request.PredictionId, ct);

        if (prediction is null)
        {
            return Results.NotFound();
        }

        if (!OwnsPrediction(prediction, user))
        {
            return Results.Forbid();
        }

        if (prediction.LockedAt is not null || MatchLockService.IsLocked(prediction.Match))
        {
            return Results.BadRequest(new { error = MatchLockService.LockReason(prediction.Match) });
        }

        prediction.PredictionValue = request.PredictionValue.Trim();
        prediction.UpdatedAt = DateTimeOffset.UtcNow;
        prediction.PointsAwarded = scoring.CalculatePoints(
            prediction.PredictionType, prediction.PredictionValue, prediction.Match);

        await db.SaveChangesAsync(ct);
        return Results.Ok(Map(prediction, prediction.Match));
    }

    private static async Task<IResult> GetPredictionHistory(
        AppDbContext db,
        IUserContext user,
        HttpContext http,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var query = db.Predictions.Include(p => p.Match).AsQueryable();
        query = user.IsAuthenticated
            ? query.Where(p => p.UserId == user.UserId)
            : query.Where(p => p.AnonymousUserId == user.AnonymousUserId);

        var predictions = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return Results.Ok(predictions.Select(p => Map(p, p.Match)));
    }

    private static bool OwnsPrediction(Prediction prediction, IUserContext user) =>
        user.IsAuthenticated
            ? prediction.UserId == user.UserId
            : prediction.AnonymousUserId == user.AnonymousUserId;

    private static PredictionResponse Map(Prediction p, Match match) =>
        new(
            p.Id,
            p.MatchId,
            p.PredictionType,
            p.PredictionValue,
            p.PointsAwarded,
            p.CreatedAt,
            p.UpdatedAt,
            MatchLockService.IsLocked(match),
            match.KickoffTime,
            new PredictionMatchSummary(
                match.TeamA,
                match.TeamB,
                match.TeamACode,
                match.TeamBCode,
                match.Status,
                match.HomeScore,
                match.AwayScore,
                match.KickoffTime));
}
