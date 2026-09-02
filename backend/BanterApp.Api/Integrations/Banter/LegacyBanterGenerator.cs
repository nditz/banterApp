using BanterApp.Api.Integrations.Media;

namespace BanterApp.Api.Integrations.Banter;

/// <summary>Legacy path: delegates media resolution to <see cref="ReactionMediaResolver"/>.</summary>
public sealed class LegacyBanterGenerator : IBanterGenerator
{
    private readonly ReactionMediaResolver _resolver;

    public LegacyBanterGenerator(ReactionMediaResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<BanterGenerationResult> GenerateAsync(
        BanterGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var media = await _resolver.ResolveAsync(
            request.SuggestedQueries,
            request.Mood ?? request.Context.MoodHint,
            request.Seed,
            cancellationToken);

        return new BanterGenerationResult(
            media.Url,
            media.Type,
            Scenario: null,
            SearchPhrase: null,
            ProviderContentId: GiphyGifSelector.FromUrl(media.Url),
            UsedLegacyPath: true,
            UsedFallback: false);
    }
}
