using BanterApp.Api.Features.Ai;
using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Integrations.SportsData.Dtos;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests;

public class PunditScriptPromptBuilderTests
{
    [Theory]
    [InlineData("touchline-uk", "Passionate")]
    [InlineData("ex-pro-couch", "Captain")]
    [InlineData("hot-take-desk", "Loud")]
    [InlineData("silky-studio", "Elegant")]
    public void BuildSystemPrompt_includes_persona_style_traits(string styleSlug, string expectedTrait)
    {
        var persona = PunditPersonas.FindByStyleSlug(styleSlug)!;
        var style = PunditStyleProfiles.Get(styleSlug);

        var prompt = PunditScriptPromptBuilder.BuildSystemPrompt("Base prompt.", persona, style);

        Assert.Contains(persona.Name, prompt);
        Assert.Contains(expectedTrait, prompt);
    }

    [Fact]
    public void BuildUserPrompt_pre_match_includes_preview_guidance()
    {
        var context = CreateSampleContext(isFinished: false);
        var persona = PunditPersonas.Defaults[0];

        var prompt = PunditScriptPromptBuilder.BuildUserPrompt(
            context, persona, "pre_match", VideoScriptDuration.Sixty);

        Assert.Contains("pre-match", prompt);
        Assert.Contains("Preview the key tactical matchup", prompt);
        Assert.Contains("England", prompt);
    }

    [Fact]
    public void BuildUserPrompt_post_match_includes_event_guidance()
    {
        var context = CreateSampleContext(isFinished: true);
        var persona = PunditPersonas.Defaults[0];

        var prompt = PunditScriptPromptBuilder.BuildUserPrompt(
            context, persona, "post_match", VideoScriptDuration.Sixty);

        Assert.Contains("post-match", prompt);
        Assert.Contains("pivotal event", prompt);
        Assert.Contains("Saka", prompt);
    }

    private static MatchScriptContext CreateSampleContext(bool isFinished)
    {
        var match = new MatchDto(
            "match-1",
            new TeamDto("t1", "England", "ENG"),
            new TeamDto("t2", "France", "FRA"),
            DateTimeOffset.UtcNow,
            "Round of 16",
            "Group A",
            "Wembley Stadium",
            isFinished ? "FT" : "NS",
            isFinished ? 2 : null,
            isFinished ? 1 : null);

        var stats = new MatchStatisticsDto(
            "match-1", 58, 42, 14, 9, 6, 3, 7, 4, 10, 12, 1, 2, 0, 0);

        var events = isFinished
            ? new List<MatchEventDto>
            {
                new("e1", 23, "Goal", "ENG", "Saka", "Right foot"),
                new("e2", 67, "Goal", "FRA", "Mbappe", "Penalty"),
            }
            : [];

        return new MatchScriptContext(match, stats, events, [], []);
    }
}

public class PunditScriptComposerTests
{
    [Fact]
    public void Compose_produces_all_eight_scenes_with_metadata()
    {
        var context = new MatchScriptContext(
            new MatchDto(
                "m1",
                new TeamDto("t1", "England", "ENG"),
                new TeamDto("t2", "France", "FRA"),
                DateTimeOffset.UtcNow,
                "Final",
                "Group A",
                "Wembley",
                "FT",
                2,
                1),
            new MatchStatisticsDto("m1", 55, 45, 12, 8, 5, 4, 6, 5, 9, 11, 0, 1, 0, 0),
            [new MatchEventDto("e1", 10, "Goal", "ENG", "Kane", null)],
            [new LineupPlayerDto("ENG", 9, "Kane", "FW", false)],
            []);

        var script = PunditScriptComposer.Compose(
            context,
            PunditPersonas.Defaults[0],
            "post_match",
            VideoScriptDuration.Sixty);

        for (var i = 1; i <= 8; i++)
        {
            Assert.Contains($"SCENE {i}:", script);
        }

        Assert.Contains("[Visual:", script);
        Assert.Contains("DIALOGUE:", script);
        Assert.Contains("Side-View Gary", script);
    }
}

public class PunditScriptRequestValidatorTests
{
    private readonly PunditScriptRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.TestValidate(new PunditScriptRequest(
            "match-1", "pre_match", "touchline-uk", VideoScriptDuration.Sixty));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("motd")]
    public void Unknown_style_slug_fails(string styleSlug)
    {
        var result = _validator.TestValidate(new PunditScriptRequest(
            "match-1", "pre_match", styleSlug));

        result.ShouldHaveValidationErrorFor(x => x.StyleSlug);
    }

    [Theory]
    [InlineData("halftime")]
    [InlineData("")]
    public void Invalid_phase_fails(string phase)
    {
        var result = _validator.TestValidate(new PunditScriptRequest(
            "match-1", phase, "touchline-uk"));

        result.ShouldHaveValidationErrorFor(x => x.Phase);
    }
}

public class MatchScriptContextBuilderTests
{
    [Fact]
    public async Task BuildAsync_returns_null_when_match_not_found()
    {
        var sports = new StubSportsProvider([], []);
        var services = new ServiceCollection().BuildServiceProvider();
        var builder = new MatchScriptContextBuilder(sports, services);

        var result = await builder.BuildAsync("missing", "pre_match");

        Assert.Null(result);
    }

    [Fact]
    public async Task BuildAsync_handles_missing_enrichment_gracefully()
    {
        var match = new MatchDto(
            "m1",
            new TeamDto("t1", "England", "ENG"),
            new TeamDto("t2", "France", "FRA"),
            DateTimeOffset.UtcNow,
            "Group Stage",
            "Group A",
            "Wembley",
            "FT",
            1,
            0);

        var sports = new StubSportsProvider([match], []);
        var services = new ServiceCollection().BuildServiceProvider();
        var builder = new MatchScriptContextBuilder(sports, services);

        var result = await builder.BuildAsync("m1", "post_match");

        Assert.NotNull(result);
        Assert.Equal("m1", result!.Match.Id);
        Assert.Empty(result.Events);
        Assert.Empty(result.Lineups);
    }

    private sealed class StubSportsProvider : ISportsDataProvider
    {
        private readonly IReadOnlyList<MatchDto> _upcoming;
        private readonly IReadOnlyList<MatchDto> _results;

        public StubSportsProvider(IReadOnlyList<MatchDto> upcoming, IReadOnlyList<MatchDto> results)
        {
            _upcoming = upcoming;
            _results = results;
        }

        public Task<IReadOnlyList<MatchDto>> GetAllFixturesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MatchDto>>(_upcoming.Concat(_results).ToList());

        public Task<IReadOnlyList<MatchDto>> GetUpcomingFixturesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_upcoming);

        public Task<IReadOnlyList<MatchDto>> GetResultsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_results);

        public Task<IReadOnlyList<MatchDto>> GetLiveFixturesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MatchDto>>([]);

        public Task<MatchStatisticsDto?> GetMatchStatisticsAsync(string matchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MatchStatisticsDto?>(null);

        public Task<IReadOnlyList<StandingDto>> GetStandingsAsync(string group, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StandingDto>>([]);
    }
}
