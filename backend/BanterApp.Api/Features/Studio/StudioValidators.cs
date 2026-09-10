using FluentValidation;

namespace BanterApp.Api.Features.Studio;

public sealed class CreateStudioPackRequestValidator : AbstractValidator<CreateStudioPackRequest>
{
    public CreateStudioPackRequestValidator()
    {
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(t => StudioContentCatalog.ContentTypes.Contains(t))
            .WithMessage("ContentType must be short, podcast, meme, caption, thread, carousel, or commentary.");
        RuleFor(x => x.Tone)
            .NotEmpty()
            .Must(t => StudioContentCatalog.Tones.Contains(t))
            .WithMessage("Tone must be a known Studio tone.");
        RuleFor(x => x.FeedItemId).MaximumLength(64);
        RuleFor(x => x)
            .Must(x => x.ReceiptId.HasValue || !string.IsNullOrWhiteSpace(x.FeedItemId) || x.ProjectId.HasValue)
            .WithMessage("Select a receipt, trending story, or saved project.");
    }
}
