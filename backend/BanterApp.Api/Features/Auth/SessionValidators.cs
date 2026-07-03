using BanterApp.Api.Services;
using FluentValidation;

namespace BanterApp.Api.Features.Auth;

public sealed class SessionConsentRequestValidator : AbstractValidator<SessionConsentRequest>
{
    public SessionConsentRequestValidator()
    {
        RuleFor(x => x.AcceptedTerms).Equal(true);
        RuleFor(x => x.Username)
            .Must(u => u is null || UsernameRules.IsValidFormat(u))
            .WithMessage("Username must be 3–20 characters and use only letters A–Z and numbers 0–9.");
    }
}

public sealed class SessionRecoverRequestValidator : AbstractValidator<SessionRecoverRequest>
{
    public SessionRecoverRequestValidator()
    {
        RuleFor(x => x.RecoveryToken).NotEmpty().MaximumLength(512);
    }
}

public sealed class SetUsernameRequestValidator : AbstractValidator<SetUsernameRequest>
{
    public SetUsernameRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Must(UsernameRules.IsValidFormat)
            .WithMessage("Username must be 3–20 characters and use only letters A–Z and numbers 0–9.");
    }
}
