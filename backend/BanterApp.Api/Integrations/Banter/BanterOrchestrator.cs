using BanterApp.Api.Features.Feed;
using BanterApp.Api.Integrations.Media;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Banter;

/// <summary>
/// Strategy Engine orchestrator: classify → concepts → Giphy pools → exclude → score → weighted select.
/// Progressively falls back to legacy <see cref="ReactionMediaResolver"/> when pools are empty.
/// </summary>
public sealed class BanterOrchestrator : IBanterGenerator
{
    private readonly BanterOptions _options;
    private readonly IBanterScenarioClassifier _classifier;
    private readonly IBanterConceptGenerator _concepts;
    private readonly IBanterCandidateProvider _candidates;
    private readonly IBanterHistoryService _history;
    private readonly IBanterCandidateScorer _scorer;
    private readonly IBanterCandidateSelector _selector;
    private readonly IBanterRandom _random;
    private readonly IReactionGifLedger _ledger;
    private readonly ReactionMediaResolver _legacyResolver;
    private readonly ILogger<BanterOrchestrator> _logger;

    public BanterOrchestrator(
        IOptions<BanterOptions> options,
        IBanterScenarioClassifier classifier,
        IBanterConceptGenerator concepts,
        IBanterCandidateProvider candidates,
        IBanterHistoryService history,
        IBanterCandidateScorer scorer,
        IBanterCandidateSelector selector,
        IBanterRandom random,
        IReactionGifLedger ledger,
        ReactionMediaResolver legacyResolver,
        ILogger<BanterOrchestrator> logger)
    {
        _options = options.Value;
        _classifier = classifier;
        _concepts = concepts;
        _candidates = candidates;
        _history = history;
        _scorer = scorer;
        _selector = selector;
        _random = random;
        _ledger = ledger;
        _legacyResolver = legacyResolver;
        _logger = logger;
    }

    public async Task<BanterGenerationResult> GenerateAsync(
        BanterGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var scenario = await _classifier.ClassifyAsync(request.Context, cancellationToken);
        _logger.LogInformation(
            "BanterScenarioClassified scenario={Scenario} matchId={MatchId} category={Category}",
            scenario,
            request.Context.MatchId,
            request.Context.Category);

        var exclusions = await _history.GetExclusionsAsync(request.Context, cancellationToken);

        // Sticky seed assignment (same card keeps same GIF).
        var assigned = await _ledger.GetAssignedUrlAsync(request.Seed, cancellationToken);
        if (!string.IsNullOrWhiteSpace(assigned))
        {
            return new BanterGenerationResult(
                assigned,
                MediaTypeFor(assigned),
                scenario,
                SearchPhrase: null,
                ProviderContentId: GiphyGifSelector.FromUrl(assigned),
                UsedLegacyPath: false,
                UsedFallback: false);
        }

        var concepts = await _concepts.GenerateAsync(request.Context, scenario, exclusions, cancellationToken);
        var sampled = SampleConcepts(concepts, request.SuggestedQueries);

        var pool = await BuildPoolAsync(sampled, exclusions, strict: true, cancellationToken);
        if (pool.Count == 0)
        {
            _logger.LogInformation(
                "BanterCandidatesExcluded remaining=0; relaxing team/global exclusions.");
            pool = await BuildPoolAsync(sampled, exclusions, strict: false, cancellationToken);
        }

        var scored = _scorer.Score(request.Context, pool, exclusions);
        var selected = _selector.Select(scored);

        if (selected is null)
        {
            return await FallbackToLegacyAsync(request, scenario, "empty_pool", cancellationToken);
        }

        var claimed = await _ledger.TryClaimAsync(
            request.Seed,
            selected.Candidate.ProviderContentId,
            selected.Candidate.Url,
            cancellationToken);

        if (!claimed)
        {
            // Another card claimed this GIF; try next best unique candidates.
            foreach (var alt in scored.Where(s => s != selected))
            {
                if (await _ledger.TryClaimAsync(
                        request.Seed,
                        alt.Candidate.ProviderContentId,
                        alt.Candidate.Url,
                        cancellationToken))
                {
                    selected = alt;
                    claimed = true;
                    break;
                }
            }
        }

        if (!claimed)
        {
            return await FallbackToLegacyAsync(request, scenario, "ledger_claim_failed", cancellationToken);
        }

        await SafeRecordAsync(
            request.Context,
            scenario,
            selected,
            cancellationToken);

        _logger.LogInformation(
            "BanterCandidateSelected scenario={Scenario} provider={Provider} contentId={ContentId} score={Score:F3} query={Query}",
            scenario,
            selected.Candidate.Provider,
            selected.Candidate.ProviderContentId,
            selected.FinalScore,
            selected.Candidate.SourceQuery);

        return new BanterGenerationResult(
            selected.Candidate.Url,
            "gif",
            scenario,
            selected.Candidate.SourceQuery,
            selected.Candidate.ProviderContentId,
            UsedLegacyPath: false,
            UsedFallback: false);
    }

    private async Task<List<BanterCandidate>> BuildPoolAsync(
        IReadOnlyList<string> queries,
        BanterExclusionContext exclusions,
        bool strict,
        CancellationToken cancellationToken)
    {
        var pool = new Dictionary<string, BanterCandidate>(StringComparer.OrdinalIgnoreCase);
        var excluded = 0;

        foreach (var query in queries)
        {
            IReadOnlyList<BanterCandidate> hits;
            try
            {
                hits = await _candidates.GetCandidatesAsync(
                    query,
                    _options.CandidatesPerConcept,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Candidate provider failed for query '{Query}'.", query);
                continue;
            }

            foreach (var hit in hits)
            {
                if (strict && exclusions.IsProviderIdExcluded(hit.ProviderContentId))
                {
                    excluded++;
                    continue;
                }

                pool.TryAdd(hit.ProviderContentId, hit);
            }
        }

        if (excluded > 0)
        {
            _logger.LogInformation("BanterCandidatesExcluded count={Count} strict={Strict}", excluded, strict);
        }

        return pool.Values.ToList();
    }

    private IReadOnlyList<string> SampleConcepts(
        IReadOnlyList<BanterConcept> concepts,
        IReadOnlyList<string?>? suggestedQueries)
    {
        var phrases = concepts.Select(c => c.Phrase).ToList();

        if (suggestedQueries is not null)
        {
            foreach (var raw in suggestedQueries)
            {
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    phrases.Add(raw.Trim());
                }
            }
        }

        // Dedupe preserving order
        var unique = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var phrase in phrases)
        {
            if (seen.Add(BanterExclusionContext.NormalizePhrase(phrase)))
            {
                unique.Add(phrase);
            }
        }

        if (unique.Count == 0)
        {
            return ["football meme"];
        }

        var take = Math.Min(_options.ConceptsUsedPerGeneration, unique.Count);
        // Fisher–Yates partial shuffle via injectable RNG
        for (var i = 0; i < take; i++)
        {
            var j = i + _random.Next(unique.Count - i);
            (unique[i], unique[j]) = (unique[j], unique[i]);
        }

        return unique.Take(take).ToList();
    }

    private async Task<BanterGenerationResult> FallbackToLegacyAsync(
        BanterGenerationRequest request,
        BanterScenario scenario,
        string reason,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "BanterFallbackUsed stage=media reason={Reason} scenario={Scenario}",
            reason,
            scenario);

        var media = await _legacyResolver.ResolveAsync(
            request.SuggestedQueries,
            request.Mood ?? request.Context.MoodHint,
            request.Seed,
            cancellationToken);

        var providerId = GiphyGifSelector.FromUrl(media.Url) ?? ReactionMediaIdentity.FromUrl(media.Url);
        await SafeRecordAsync(
            request.Context,
            scenario,
            scored: null,
            url: media.Url,
            mediaType: media.Type,
            provider: media.Url.Contains("giphy.com", StringComparison.OrdinalIgnoreCase) ? "giphy" : "legacy",
            providerId: providerId,
            searchPhrase: request.SuggestedQueries?.FirstOrDefault(q => !string.IsNullOrWhiteSpace(q)),
            score: null,
            cancellationToken);

        return new BanterGenerationResult(
            media.Url,
            media.Type,
            scenario,
            SearchPhrase: null,
            ProviderContentId: providerId,
            UsedLegacyPath: true,
            UsedFallback: true,
            FallbackReason: reason);
    }

    private Task SafeRecordAsync(
        BanterContext context,
        BanterScenario scenario,
        ScoredBanterCandidate selected,
        CancellationToken cancellationToken) =>
        SafeRecordAsync(
            context,
            scenario,
            selected,
            selected.Candidate.Url,
            "gif",
            selected.Candidate.Provider,
            selected.Candidate.ProviderContentId,
            selected.Candidate.SourceQuery,
            (decimal)selected.FinalScore,
            cancellationToken);

    private async Task SafeRecordAsync(
        BanterContext context,
        BanterScenario scenario,
        ScoredBanterCandidate? scored,
        string? url = null,
        string? mediaType = null,
        string? provider = null,
        string? providerId = null,
        string? searchPhrase = null,
        decimal? score = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _history.RecordAsync(
                new BanterSelection(
                    context,
                    scenario,
                    mediaType ?? scored?.Candidate.ContentType.ToString() ?? "gif",
                    provider ?? scored?.Candidate.Provider ?? "unknown",
                    providerId ?? scored?.Candidate.ProviderContentId,
                    searchPhrase ?? scored?.Candidate.SourceQuery,
                    MemeTemplateId: null,
                    CaptionHash: null,
                    SelectionScore: score ?? (scored is null ? null : (decimal)scored.FinalScore),
                    url ?? scored?.Candidate.Url),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to persist banter selection metadata.");
        }
    }

    private static string MediaTypeFor(string url) =>
        url.Contains("giphy.com", StringComparison.OrdinalIgnoreCase) ||
        FeedGifCatalog.IsBundledSticker(url)
            ? "gif"
            : "image";
}
