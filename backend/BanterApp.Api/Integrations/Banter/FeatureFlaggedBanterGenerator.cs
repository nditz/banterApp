using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Banter;

/// <summary>
/// Routes to the Strategy Engine orchestrator when <see cref="BanterOptions.UseStrategyEngine"/> is true;
/// otherwise uses the legacy resolver path.
/// </summary>
public sealed class FeatureFlaggedBanterGenerator : IBanterGenerator
{
    private readonly BanterOptions _options;
    private readonly LegacyBanterGenerator _legacy;
    private readonly BanterOrchestrator _orchestrator;
    private readonly ILogger<FeatureFlaggedBanterGenerator> _logger;

    public FeatureFlaggedBanterGenerator(
        IOptions<BanterOptions> options,
        LegacyBanterGenerator legacy,
        BanterOrchestrator orchestrator,
        ILogger<FeatureFlaggedBanterGenerator> logger)
    {
        _options = options.Value;
        _legacy = legacy;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public Task<BanterGenerationResult> GenerateAsync(
        BanterGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.UseStrategyEngine)
        {
            _logger.LogDebug("Banter generation using legacy path (UseStrategyEngine=false).");
            return _legacy.GenerateAsync(request, cancellationToken);
        }

        _logger.LogDebug("Banter generation using strategy engine.");
        return _orchestrator.GenerateAsync(request, cancellationToken);
    }
}
