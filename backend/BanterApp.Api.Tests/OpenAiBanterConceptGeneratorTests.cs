using System.Net;
using System.Net.Http;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Banter;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public class OpenAiBanterConceptGeneratorTests
{
    [Fact]
    public void ParseConcepts_ValidStructuredOutput_ReturnsPhrases()
    {
        var json =
            """
            {
              "scenario": "PredictionAgedBadly",
              "concepts": [
                "delete tweet reaction",
                "walk of shame",
                { "phrase": "hiding embarrassment", "tone": "roast" }
              ]
            }
            """;

        var concepts = OpenAiBanterConceptGenerator.ParseConcepts(json);

        Assert.Equal(3, concepts.Count);
        Assert.Equal("delete tweet reaction", concepts[0].Phrase);
        Assert.Equal("hiding embarrassment", concepts[2].Phrase);
        Assert.Equal("roast", concepts[2].Tone);
    }

    [Fact]
    public void ParseConcepts_MalformedOutput_ReturnsEmpty()
    {
        Assert.Empty(OpenAiBanterConceptGenerator.ParseConcepts("{ not json"));
        Assert.Empty(OpenAiBanterConceptGenerator.ParseConcepts("""{"foo":1}"""));
    }

    [Fact]
    public void Normalize_RemovesDuplicatesAndExcludedPhrases()
    {
        var exclusions = new BanterExclusionContext();
        exclusions.SearchPhrases.Add("walk of shame");

        var normalized = OpenAiBanterConceptGenerator.Normalize(
            [
                new BanterConcept("Delete Tweet Reaction"),
                new BanterConcept("delete tweet reaction"),
                new BanterConcept("walk of shame"),
                new BanterConcept("   "),
                new BanterConcept(new string('x', 100))
            ],
            exclusions,
            targetCount: 8,
            BanterScenario.PredictionAgedBadly);

        Assert.Contains(normalized, c => c.Phrase.Equals("Delete Tweet Reaction", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(normalized, c => BanterExclusionContext.NormalizePhrase(c.Phrase) == "walk of shame");
        Assert.True(normalized.Count >= 1);
        Assert.True(normalized.Count <= 8);
        Assert.Equal(normalized.Count, normalized.Select(c => BanterExclusionContext.NormalizePhrase(c.Phrase)).Distinct().Count());
    }

    [Fact]
    public void Normalize_TooFewConcepts_MergesPredefinedFallbacks()
    {
        var normalized = OpenAiBanterConceptGenerator.Normalize(
            [new BanterConcept("only one")],
            BanterExclusionContext.Empty,
            targetCount: 6,
            BanterScenario.Overconfidence);

        Assert.True(normalized.Count >= 6);
        Assert.Contains(normalized, c => c.Phrase.Equals("only one", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PredefinedFallbacks_ExistForKnownScenarios()
    {
        var concepts = PredefinedBanterConcepts.ForScenario(BanterScenario.BottledIt, 5);
        Assert.Equal(5, concepts.Count);
        Assert.All(concepts, c => Assert.False(string.IsNullOrWhiteSpace(c.Phrase)));
    }

    [Fact]
    public async Task GenerateAsync_OpenAiHttpFailure_FallsBackToPredefined()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("""{"error":"boom"}""")
        });
        var http = new HttpClient(handler);
        var sut = new OpenAiBanterConceptGenerator(
            http,
            Options.Create(new AiOptions { ApiKey = "test-key", Model = "gpt-4o-mini", MaxTokens = 200 }),
            Options.Create(new BanterOptions { ConceptCount = 6 }),
            NullLogger<OpenAiBanterConceptGenerator>.Instance);

        var concepts = await sut.GenerateAsync(
            new BanterContext(
                null, null, null, null, null, null,
                PredictionOutcomeKind.Unknown, MatchOutcomeKind.Unknown,
                null, null, null),
            BanterScenario.Overconfidence,
            BanterExclusionContext.Empty);

        Assert.True(concepts.Count >= 6);
        Assert.Contains(concepts, c => c.Phrase.Contains("mistake", StringComparison.OrdinalIgnoreCase)
            || c.Phrase.Contains("shame", StringComparison.OrdinalIgnoreCase)
            || c.Phrase.Contains("cringe", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
