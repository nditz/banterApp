using FluentValidation;

namespace BanterApp.Api.Features.Leagues;

public sealed class CreateLeagueRequestValidator : AbstractValidator<CreateLeagueRequest>
{
    public CreateLeagueRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(40);
    }
}

public sealed class JoinLeagueRequestValidator : AbstractValidator<JoinLeagueRequest>
{
    public JoinLeagueRequestValidator()
    {
        RuleFor(x => x.InviteCode).NotEmpty().MaximumLength(12);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(40);
    }
}
