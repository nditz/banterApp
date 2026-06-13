using FluentValidation;

namespace BanterApp.Api.Features.Auth;

public sealed class SessionConsentRequestValidator : AbstractValidator<SessionConsentRequest>
{
    public SessionConsentRequestValidator()
    {
        RuleFor(x => x.AcceptedTerms).Equal(true);
    }
}

public sealed class SessionRecoverRequestValidator : AbstractValidator<SessionRecoverRequest>
{
    public SessionRecoverRequestValidator()
    {
        RuleFor(x => x.RecoveryToken).NotEmpty().MaximumLength(512);
    }
}
