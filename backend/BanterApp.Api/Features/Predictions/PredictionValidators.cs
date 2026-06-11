using BanterApp.Api.Data.Entities;
using FluentValidation;

namespace BanterApp.Api.Features.Predictions;

public sealed class CreatePredictionRequestValidator : AbstractValidator<CreatePredictionRequest>
{
    public CreatePredictionRequestValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PredictionValue).NotEmpty().MaximumLength(32);
        RuleFor(x => x.PredictionType).IsInEnum();
        RuleFor(x => x).Must(HasValidValue).WithMessage("Prediction value is invalid for the selected type.");
    }

    private static bool HasValidValue(CreatePredictionRequest request) =>
        request.PredictionType switch
        {
            PredictionType.Result => IsValidResult(request.PredictionValue),
            PredictionType.CorrectScore => IsValidScore(request.PredictionValue),
            PredictionType.DoubleChance => IsValidDoubleChance(request.PredictionValue),
            _ => false
        };

    private static bool IsValidResult(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        return v is "H" or "D" or "A" or "HOME" or "DRAW" or "AWAY" or "1" or "X" or "2";
    }

    private static bool IsValidScore(string value)
    {
        var parts = value.Split('-', ':');
        return parts.Length == 2 && int.TryParse(parts[0].Trim(), out _) && int.TryParse(parts[1].Trim(), out _);
    }

    private static bool IsValidDoubleChance(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        return v is "HD" or "DA" or "HA" or "H/D" or "A/D" or "H/A" or "1X" or "X2" or "12"
            or "HOME OR DRAW" or "AWAY OR DRAW" or "HOME OR AWAY";
    }
}

public sealed class UpdatePredictionRequestValidator : AbstractValidator<UpdatePredictionRequest>
{
    public UpdatePredictionRequestValidator()
    {
        RuleFor(x => x.PredictionId).NotEmpty();
        RuleFor(x => x.PredictionValue).NotEmpty().MaximumLength(32);
    }
}
