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
            .WithValidation<CreatePredictionRequest>();
        group.MapPut("/update", UpdatePrediction)
            .WithValidation<UpdatePredictionRequest>();
        group.MapGet("/history", GetPredictionHistory);

        return app;
    }

    private static async Task<IResult> CreatePrediction(
        CreatePredictionRequest request,
        AppDbContext db,
        IUserContext user,
        ScoringService scoring,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return Results.Unauthorized();
        }

        var match = await db.Matches.FindAsync([request.MatchId], ct);
        if (match is null)
        {
            return Results.NotFound(new { error = "Match not found." });
        }

        if (match.Status == "FT" && match.KickoffTime <= DateTimeOffset.UtcNow)
        {
            return Results.BadRequest(new { error = "Cannot predict on a finished match." });
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

        return Results.Created($"/api/predictions/history", Map(prediction));
    }

    private static async Task<IResult> UpdatePrediction(
        UpdatePredictionRequest request,
        AppDbContext db,
        IUserContext user,
        ScoringService scoring,
        CancellationToken ct)
    {
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

        if (prediction.Match.Status == "FT")
        {
            return Results.BadRequest(new { error = "Cannot update prediction for a finished match." });
        }

        prediction.PredictionValue = request.PredictionValue.Trim();
        prediction.UpdatedAt = DateTimeOffset.UtcNow;
        prediction.PointsAwarded = scoring.CalculatePoints(
            prediction.PredictionType, prediction.PredictionValue, prediction.Match);

        await db.SaveChangesAsync(ct);
        return Results.Ok(Map(prediction));
    }

    private static async Task<IResult> GetPredictionHistory(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return Results.Unauthorized();
        }

        var query = db.Predictions.AsQueryable();
        query = user.IsAuthenticated
            ? query.Where(p => p.UserId == user.UserId)
            : query.Where(p => p.AnonymousUserId == user.AnonymousUserId);

        var predictions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => Map(p))
            .ToListAsync(ct);

        return Results.Ok(predictions);
    }

    private static bool OwnsPrediction(Prediction prediction, IUserContext user) =>
        user.IsAuthenticated
            ? prediction.UserId == user.UserId
            : prediction.AnonymousUserId == user.AnonymousUserId;

    private static PredictionResponse Map(Prediction p) =>
        new(p.Id, p.MatchId, p.PredictionType, p.PredictionValue, p.PointsAwarded, p.CreatedAt, p.UpdatedAt);
}
