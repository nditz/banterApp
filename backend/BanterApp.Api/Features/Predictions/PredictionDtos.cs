using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Predictions;

public sealed record CreatePredictionRequest(
    string MatchId,
    PredictionType PredictionType,
    string PredictionValue,
    string? TurnstileToken = null);

public sealed record UpdatePredictionRequest(
    Guid PredictionId,
    string PredictionValue,
    string? TurnstileToken = null);

public sealed record PredictionResponse(
    Guid Id,
    string MatchId,
    PredictionType PredictionType,
    string PredictionValue,
    int PointsAwarded,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsLocked,
    DateTimeOffset KickoffTime);
