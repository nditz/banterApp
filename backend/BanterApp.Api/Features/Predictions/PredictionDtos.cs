using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Predictions;

public sealed record CreatePredictionRequest(
    string MatchId,
    PredictionType PredictionType,
    string PredictionValue);

public sealed record UpdatePredictionRequest(
    Guid PredictionId,
    string PredictionValue);

public sealed record PredictionResponse(
    Guid Id,
    string MatchId,
    PredictionType PredictionType,
    string PredictionValue,
    int PointsAwarded,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
