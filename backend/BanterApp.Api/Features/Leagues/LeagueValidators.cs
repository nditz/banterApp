using BanterApp.Api.Common;
using FluentValidation;

namespace BanterApp.Api.Features.Leagues;

public sealed class CreateLeagueRequestValidator : AbstractValidator<CreateLeagueRequest>
{
    public CreateLeagueRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(StringLimits.LeagueName)
            .Must(name => !ProfanityFilter.ContainsProfanity(name))
            .WithMessage("League name contains language we can't allow on a family-friendly site.");
    }
}

public sealed class JoinLeagueRequestValidator : AbstractValidator<JoinLeagueRequest>
{
    public JoinLeagueRequestValidator()
    {
        RuleFor(x => x.InviteCode).NotEmpty().MaximumLength(12);
    }
}
