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
        RuleFor(x => x.Context).NotEmpty().MaximumLength(4000);
    }
}

public sealed class VideoScriptRequestValidator : AbstractValidator<VideoScriptRequest>
{
    public VideoScriptRequestValidator()
    {
        RuleFor(x => x.Context).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Format).IsInEnum();
        RuleFor(x => x.Duration).IsInEnum();
    }
}

public sealed class BroadcastScriptRequestValidator : AbstractValidator<BroadcastScriptRequest>
{
    public BroadcastScriptRequestValidator()
    {
        RuleFor(x => x.Phase)
            .NotEmpty()
            .Must(p => p is "pre_match" or "post_match")
            .WithMessage("Phase must be 'pre_match' or 'post_match'.");
        RuleFor(x => x.Style)
            .Must(s => s is null or "full" or "praise" or "burn")
            .WithMessage("Style must be 'full', 'praise', or 'burn'.");
        RuleFor(x => x.Picks).NotNull();
        RuleFor(x => x.Picks.Count).LessThanOrEqualTo(30);
        RuleForEach(x => x.Picks).ChildRules(pick =>
        {
            pick.RuleFor(p => p.TeamA).NotEmpty().MaximumLength(100);
            pick.RuleFor(p => p.TeamB).NotEmpty().MaximumLength(100);
            pick.RuleFor(p => p.Prediction).NotEmpty().MaximumLength(100);
        });
    }
}

public sealed class PunditScriptRequestValidator : AbstractValidator<PunditScriptRequest>
{
    private static readonly HashSet<string> ValidStyleSlugs =
        Pundits.PunditPersonas.Defaults.Select(p => p.StyleSlug).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public PunditScriptRequestValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phase)
            .NotEmpty()
            .Must(p => p is "pre_match" or "post_match")
            .WithMessage("Phase must be 'pre_match' or 'post_match'.");
        RuleFor(x => x.StyleSlug)
            .NotEmpty()
            .Must(s => ValidStyleSlugs.Contains(s))
            .WithMessage("StyleSlug must be a known pundit persona style.");
        RuleFor(x => x.Duration).IsInEnum();
    }
}
