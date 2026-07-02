using System.Collections.Concurrent;
using BanterApp.Api.Features.Ai;
using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Integrations.FootballBanter;
using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Integrations.Ai;

/// <summary>
/// Phase 1 stub content generator. Returns canned PG-rated template strings — no live LLM calls.
/// </summary>
public sealed class StubContentGenerator : IContentGenerator
{
    private const int AnonymousGenerationLimit = 3;

    private readonly ConcurrentDictionary<string, int> _generationCounts = new();

    private static readonly Dictionary<BanterTone, string[]> BanterTemplates = new()
    {
        [BanterTone.Friendly] =
        [
            "You picked {prediction} and the final was {result}. Solid read — your football brain is switched on today.",
            "Not bad at all! You went with {prediction}, it ended {result}. Your mates might actually listen to you next time.",
            "Respectable call: {prediction} vs reality {result}. You're building a decent prediction CV here.",
            "You called {prediction} — final score {result}. That's the kind of take that earns you bragging rights in the group chat.",
        ],
        [BanterTone.Roast] =
        [
            "You said {prediction}. The match said {result}. Your crystal ball needs a factory reset.",
            "Bold pick: {prediction}. Reality: {result}. That prediction aged like leftover stadium nachos.",
            "{prediction}? The universe delivered {result} instead. Even the VAR screen is laughing.",
            "You went all-in on {prediction}. Final whistle: {result}. Time to update your football résumé.",
            "Your {prediction} take met {result} on the pitch. Spoiler: the pitch won.",
        ],
        [BanterTone.Praise] =
        [
            "LEGEND. You nailed {prediction} and it finished {result}. The pundits wish they had your instincts.",
            "Absolute masterclass — {prediction} locked in, {result} confirmed. You're basically a World Cup oracle.",
            "You called {prediction} before anyone believed it. {result} at full time. Take a bow.",
            "Chef's kiss prediction: {prediction}. Result: {result}. You're cooking with gas.",
        ],
    };

    private static readonly string[] AnalysisTemplates =
    [
        "You predicted {prediction}. The stats tell the story: {home} had {homePoss}% possession and {homeShots} shots " +
        "({homeOnTarget} on target) vs {away}'s {awayPoss}% and {awayShots} shots. " +
        "The numbers {verdict} your call.",

        "Looking at your {prediction} pick through the data lens: possession split {homePoss}/{awayPoss}, " +
        "shots {homeShots}-{awayShots}, on target {homeOnTarget}-{awayOnTarget}. " +
        "{verdict}",

        "Pundit breakdown for your {prediction} prediction: corners {homeCorners}-{awayCorners}, " +
        "fouls {homeFouls}-{awayFouls}. The underlying metrics {verdict}.",

        "Your {prediction} read vs the box score — {homeShots} vs {awayShots} shots, " +
        "{homeOnTarget}-{awayOnTarget} on target. {verdict}",

        "From a tactical view on your {prediction} call: {home} controlled {homePoss}% of the ball. " +
        "Cards were {homeYellows}-{awayYellows} yellows. {verdict}",
    ];

    private static readonly string[] MemeCaptionTemplates =
    [
        "POV: You predicted {context} and the group chat hasn't recovered.",
        "When you said {context} and everyone thought you were joking.",
        "Me explaining why {context} was obvious all along.",
        "That moment when {context} happens and you pretend you saw it coming.",
        "Nobody: ... You after {context}: 'I told you so.'",
    ];

    private static readonly Dictionary<(VideoScriptFormat, VideoScriptDuration), string[]> VideoScriptTemplates = new()
    {
        [(VideoScriptFormat.TikTok, VideoScriptDuration.Fifteen)] =
        [
            "[0-3s] HOOK: '{context}' — did you see that coming?\n[3-12s] Quick recap + your prediction vs result.\n[12-15s] CTA: Drop your pick for the next match!",
            "[0-2s] 'Wait for it...' — {context}\n[2-13s] Fast cuts, score graphic, reaction face.\n[13-15s] Follow for daily World Cup banter.",
        ],
        [(VideoScriptFormat.TikTok, VideoScriptDuration.Thirty)] =
        [
            "[0-5s] HOOK: The wildest take on {context}.\n[5-22s] Setup → prediction → result → reaction.\n[22-30s] CTA + league invite link in bio.",
            "[0-4s] 'Everyone laughed at this prediction...'\n[4-24s] Story arc around {context} with stats overlay.\n[24-30s] Join our prediction league — link below.",
            "[0-5s] Bold claim about {context}.\n[5-25s] Match highlights recap with your score overlay.\n[25-30s] Who got it right? Comment below.",
        ],
        [(VideoScriptFormat.TikTok, VideoScriptDuration.Sixty)] =
        [
            "[0-8s] HOOK + intro: 'Let's talk about {context}.'\n[8-45s] Deep dive: prediction, key moments, stats, banter.\n[45-60s] Recap + CTA to predict the next fixture.",
        ],
        [(VideoScriptFormat.YouTubeShort, VideoScriptDuration.Fifteen)] =
        [
            "[0-3s] Title card: {context}\n[3-12s] One-stat breakdown + punchline.\n[12-15s] Subscribe for World Cup predictions.",
        ],
        [(VideoScriptFormat.YouTubeShort, VideoScriptDuration.Thirty)] =
        [
            "[0-5s] 'You won't believe this prediction...' — {context}\n[5-25s] Prediction vs reality with on-screen stats.\n[25-30s] Full analysis on the channel — subscribe.",
            "[0-4s] Cold open on {context}.\n[4-26s] Three key facts + your take.\n[26-30s] Predict with us — link in description.",
        ],
        [(VideoScriptFormat.YouTubeShort, VideoScriptDuration.Sixty)] =
        [
            "[0-10s] Intro hook on {context}.\n[10-50s] Segment 1: prediction. Segment 2: stats. Segment 3: verdict.\n[50-60s] Subscribe + next match preview.",
        ],
        [(VideoScriptFormat.Instagram, VideoScriptDuration.Fifteen)] =
        [
            "[Reel] Visual: score graphic. VO: '{context} — here's what happened.' End card: 'Predict the next one → link in bio.'",
        ],
        [(VideoScriptFormat.Instagram, VideoScriptDuration.Thirty)] =
        [
            "[Reel] Slide 1: Your prediction. Slide 2: The result. Slide 3: Stats snapshot. Slide 4: CTA — join our league.",
            "[Reel] Trending audio under {context} recap. Text overlays for key stats. End: 'Tag a friend who got it wrong.'",
        ],
        [(VideoScriptFormat.Instagram, VideoScriptDuration.Sixty)] =
        [
            "[Reel] Full mini-story: setup ({context}), conflict (wrong/right prediction), resolution (final score + banter). CTA on last frame.",
        ],
    };

    public Task<bool> CanGenerateAsync(
        string? userId,
        bool isAnonymous,
        CancellationToken cancellationToken = default)
    {
        if (!isAnonymous)
        {
            return Task.FromResult(true);
        }

        var key = ResolveUserKey(userId, isAnonymous);
        var count = _generationCounts.GetValueOrDefault(key, 0);
        return Task.FromResult(count < AnonymousGenerationLimit);
    }

    public async Task<string> GenerateBanterAsync(
        string userPrediction,
        string actualResult,
        BanterTone tone = BanterTone.Friendly,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);

        var templates = BanterTemplates[tone];
        var template = PickTemplate(templates, userPrediction, actualResult);

        return template
            .Replace("{prediction}", userPrediction, StringComparison.Ordinal)
            .Replace("{result}", actualResult, StringComparison.Ordinal);
    }

    public async Task<string> GenerateAnalysisAsync(
        string userPrediction,
        MatchStatisticsDto matchStats,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);

        var template = PickTemplate(AnalysisTemplates, userPrediction, matchStats.MatchId);
        var verdict = BuildAnalysisVerdict(userPrediction, matchStats);

        return template
            .Replace("{prediction}", userPrediction, StringComparison.Ordinal)
            .Replace("{home}", "Home", StringComparison.Ordinal)
            .Replace("{away}", "Away", StringComparison.Ordinal)
            .Replace("{homePoss}", matchStats.HomePossessionPercent.ToString(), StringComparison.Ordinal)
            .Replace("{awayPoss}", matchStats.AwayPossessionPercent.ToString(), StringComparison.Ordinal)
            .Replace("{homeShots}", matchStats.HomeShots.ToString(), StringComparison.Ordinal)
            .Replace("{awayShots}", matchStats.AwayShots.ToString(), StringComparison.Ordinal)
            .Replace("{homeOnTarget}", matchStats.HomeShotsOnTarget.ToString(), StringComparison.Ordinal)
            .Replace("{awayOnTarget}", matchStats.AwayShotsOnTarget.ToString(), StringComparison.Ordinal)
            .Replace("{homeCorners}", matchStats.HomeCorners.ToString(), StringComparison.Ordinal)
            .Replace("{awayCorners}", matchStats.AwayCorners.ToString(), StringComparison.Ordinal)
            .Replace("{homeFouls}", matchStats.HomeFouls.ToString(), StringComparison.Ordinal)
            .Replace("{awayFouls}", matchStats.AwayFouls.ToString(), StringComparison.Ordinal)
            .Replace("{homeYellows}", matchStats.HomeYellowCards.ToString(), StringComparison.Ordinal)
            .Replace("{awayYellows}", matchStats.AwayYellowCards.ToString(), StringComparison.Ordinal)
            .Replace("{verdict}", verdict, StringComparison.Ordinal);
    }

    public async Task<string> GenerateMemeCaptionAsync(
        string context,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);

        var template = PickTemplate(MemeCaptionTemplates, context);
        return template.Replace("{context}", context, StringComparison.Ordinal);
    }

    public async Task<string> GenerateVideoScriptAsync(
        VideoScriptFormat format,
        VideoScriptDuration duration,
        string context,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);

        var key = (format, duration);
        if (!VideoScriptTemplates.TryGetValue(key, out var templates))
        {
            templates = VideoScriptTemplates[(VideoScriptFormat.TikTok, VideoScriptDuration.Thirty)];
        }

        var template = PickTemplate(templates, context);
        return template.Replace("{context}", context, StringComparison.Ordinal);
    }

    public async Task<string> GeneratePunditScriptAsync(
        MatchScriptContext context,
        PunditPersonaSeed persona,
        string phase,
        VideoScriptDuration duration,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanGenerateAsync(userId, isAnonymous, cancellationToken);
        return PunditScriptComposer.Compose(context, persona, phase, duration);
    }

    private async Task EnsureCanGenerateAsync(
        string? userId,
        bool isAnonymous,
        CancellationToken cancellationToken)
    {
        if (!await CanGenerateAsync(userId, isAnonymous, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Anonymous users are limited to {AnonymousGenerationLimit} AI content generations.");
        }

        if (isAnonymous)
        {
            var key = ResolveUserKey(userId, isAnonymous);
            _generationCounts.AddOrUpdate(key, 1, static (_, current) => current + 1);
        }
    }

    private static readonly string[] NewsReactionTemplates =
    [
        "Bro {headline}?? The timeline is COOKED and I'm here for it. {summary} No cap this is main-character arc stuff.",
        "Hold up — {headline}. Football Twitter about to ratio someone and I'm grabbing popcorn. {summary}",
        "Lowkey {headline} has the group chat in shambles. {summary} It's giving delulu season energy fr.",
        "Not me reading '{headline}' and immediately checking who said it out loud. {summary} The banter writes itself.",
        "POV: you wake up to '{headline}' and pretend you saw it coming. {summary} VAR couldn't save this narrative.",
    ];

    private static readonly string[] FeedBanterTitleTemplates =
    [
        "No cap: {hook} 🚫🧢",
        "{hook} — the timeline is COOKED 🔥",
        "It's giving {vibe} energy: {hook}",
        "Bro really said {hook} 💀",
        "{hook} and I'm not okay",
    ];

    private static readonly string[] FeedBanterBodyTemplates =
    [
        "Lowkey {summary} — football Twitter is about to have a field day with this one.",
        "Not me reading this and immediately opening the group chat. {summary}",
        "The banter writes itself: {summary} Main character energy, zero cap.",
        "This is giving chaos ball knowledge. {summary}",
        "POV: you saw this headline and knew the memes were coming. {summary}",
    ];

    private static readonly string[] FootballJokeLines =
    [
        "Me explaining why this was obvious all along (it wasn't).",
        "When the pundit is cooking and the defence is not.",
        "That face you make when the transfer rumour was actually true.",
        "Nobody: … Football Twitter: allow me to introduce myself.",
        "VAR review on my ability to cope with this headline.",
    ];

    public Task<string> GenerateNewsReactionAsync(
        string headline,
        string summary,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var template = PickTemplate(NewsReactionTemplates, headline, category ?? "news");
        var body = template
            .Replace("{headline}", headline.Trim(), StringComparison.Ordinal)
            .Replace("{summary}", summary.Trim(), StringComparison.Ordinal);

        if (!string.IsNullOrWhiteSpace(category) && category.StartsWith("match_", StringComparison.Ordinal))
        {
            body += " Lock in your picks before kickoff — I know ball, watch me.";
        }

        return Task.FromResult(body);
    }

    public Task<string?> GenerateReactionImageUrlAsync(
        string headline,
        string reactionText,
        string? category = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task<FeedVisualSuggestion> SuggestFeedVisualAsync(
        string headline,
        string reactionText,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var seed = $"{headline}|{reactionText}|{category}";
        var moods = new[] { "celebrate", "debate", "shock", "facepalm", "hype", "pundit", "news", "cooked", "ratio" };
        var mood = moods[Math.Abs(seed.GetHashCode()) % moods.Length];
        var useGif = Math.Abs(seed.GetHashCode()) % 4 != 0;
        return Task.FromResult(useGif
            ? new FeedVisualSuggestion("gif", mood, null)
            : new FeedVisualSuggestion("image", null, $"Football banter: {headline}"));
    }

    public Task<FeedBanterCard> GenerateFeedBanterCardAsync(
        string headline,
        string summary,
        string? category = null,
        string? author = null,
        CancellationToken cancellationToken = default)
    {
        var hook = headline.Length > 72 ? headline[..69] + "…" : headline;
        var vibe = category switch
        {
            "pundit_quote" => "hot take",
            "match_live" => "matchday chaos",
            "match_result" => "full-time meltdown",
            "match_fixture" => "fixture anxiety",
            _ => "headline",
        };

        var titleTemplate = category == "pundit_quote" && !string.IsNullOrWhiteSpace(author)
            ? "{author} said WHAT now? 💀"
            : PickTemplate(FeedBanterTitleTemplates, headline, category ?? "news");

        var title = titleTemplate
            .Replace("{hook}", hook, StringComparison.Ordinal)
            .Replace("{vibe}", vibe, StringComparison.Ordinal)
            .Replace("{author}", author?.Trim() ?? "This pundit", StringComparison.Ordinal);

        var body = PickTemplate(FeedBanterBodyTemplates, summary, category ?? "news")
            .Replace("{summary}", summary.Trim(), StringComparison.Ordinal);

        if (!string.IsNullOrWhiteSpace(author) && category == "pundit_quote")
        {
            body += $"\n\n— {author.Trim()} via the football industrial complex (real take, no cap).";
        }

        var jokeLine = PickTemplate(FootballJokeLines, headline);
        var moods = new[] { "celebrate", "debate", "shock", "facepalm", "hype", "pundit", "cooked", "ratio", "delulu" };
        var mood = moods[Math.Abs($"{headline}|{category}".GetHashCode()) % moods.Length];

        return Task.FromResult(new FeedBanterCard(title, body, mood, jokeLine));
    }

    public Task<string> GenerateFootballBanterJsonAsync(
        FootballBanterSourceInput input,
        string systemPrompt,
        FootballBanterOpenAiConfig openAiConfig,
        int banterIntensity,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(FootballBanterStubOutputBuilder.BuildJson(input));

    private static string ResolveUserKey(string? userId, bool isAnonymous)
    {
        if (!isAnonymous)
        {
            return userId ?? "registered-anonymous-fallback";
        }

        return string.IsNullOrWhiteSpace(userId) ? "anonymous-guest" : $"anonymous:{userId}";
    }

    private static string PickTemplate(string[] templates, params string[] seedParts)
    {
        var seed = string.Join('|', seedParts);
        var index = Math.Abs(seed.GetHashCode()) % templates.Length;
        return templates[index];
    }

    private static string BuildAnalysisVerdict(string userPrediction, MatchStatisticsDto stats)
    {
        var homeDominant = stats.HomeShotsOnTarget > stats.AwayShotsOnTarget &&
                           stats.HomePossessionPercent >= stats.AwayPossessionPercent;

        if (userPrediction.Contains("Home", StringComparison.OrdinalIgnoreCase) && homeDominant)
        {
            return "strongly support";
        }

        if (userPrediction.Contains("Away", StringComparison.OrdinalIgnoreCase) && !homeDominant)
        {
            return "back up";
        }

        return "paint a mixed picture for";
    }
}
