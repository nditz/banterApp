using FluentValidation;

namespace BanterApp.Api.Features.Ai;

public sealed class AnalyzeRequestValidator : AbstractValidator<AnalyzeRequest>
{
    public AnalyzeRequestValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty();
        RuleFor(x => x.UserPrediction).NotEmpty().MaximumLength(200);
    }
}

public sealed class BanterRequestValidator : AbstractValidator<BanterRequest>
{
    public BanterRequestValidator()
    {
        RuleFor(x => x.UserPrediction).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ActualResult).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tone).IsInEnum();
    }
}

public sealed class MemeRequestValidator : AbstractValidator<MemeRequest>
{
    public MemeRequestValidator()
    {
        RuleFor(x => x.Context).NotEmpty().MaximumLength(500);
    }
}

public sealed class VideoScriptRequestValidator : AbstractValidator<VideoScriptRequest>
{
    public VideoScriptRequestValidator()
    {
        RuleFor(x => x.Context).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Format).IsInEnum();
        RuleFor(x => x.Duration).IsInEnum();
    }
}
