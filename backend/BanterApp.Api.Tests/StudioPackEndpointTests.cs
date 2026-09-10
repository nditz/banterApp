using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Receipts;
using BanterApp.Api.Features.Studio;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class StudioPackEndpointTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public StudioPackEndpointTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Stories_are_public_and_packs_require_session()
    {
        using var guest = _factory.CreateClient();
        var stories = await guest.GetAsync("/api/studio/stories");
        Assert.Equal(HttpStatusCode.OK, stories.StatusCode);
        var payload = await stories.Content.ReadFromJsonAsync<StudioStoriesResponse>();
        Assert.NotNull(payload);
        Assert.Empty(payload.LatestReceipts);
        Assert.Empty(payload.PreviousProjects);

        var unauth = await guest.PostAsJsonAsync("/api/studio/packs", new
        {
            contentType = "short",
            tone = "funny",
            receiptId = Guid.NewGuid()
        });
        Assert.True(
            unauth.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            await unauth.Content.ReadAsStringAsync());

        var packs = await guest.GetAsync("/api/studio/packs");
        Assert.Equal(HttpStatusCode.Unauthorized, packs.StatusCode);
    }

    [Fact]
    public async Task Receipt_to_pack_persists_sourced_facts_and_stays_private()
    {
        var owner = await _factory.CreateConsentedAnonymousClientAsync();
        var stranger = await _factory.CreateConsentedAnonymousClientAsync();
        var seeded = await SeedReceiptAsync(owner, home: 2, away: 1, pundit: true);

        var generated = await owner.PostAsJsonAsync("/api/studio/packs", new
        {
            contentType = "short",
            tone = "victory_lap",
            receiptId = seeded.ReceiptId
        });
        Assert.True(generated.IsSuccessStatusCode, await generated.Content.ReadAsStringAsync());
        var body = await generated.Content.ReadFromJsonAsync<StudioPackGenerateResponse>();
        Assert.NotNull(body?.Pack);
        Assert.Contains(body.Pack.Facts, f => f.Label == "Result" && f.Value.Contains("2–1", StringComparison.Ordinal));
        Assert.Contains(body.Pack.Facts, f => f.Provenance == "pundit_source");
        Assert.Contains("Gary Neville", body.Pack.Script, StringComparison.Ordinal);

        var listed = await owner.GetFromJsonAsync<List<StudioContentPack>>("/api/studio/packs");
        Assert.NotNull(listed);
        Assert.Contains(listed, p => p.Id == body.Pack.Id);

        var stolen = await stranger.GetAsync($"/api/studio/packs/{body.Pack.Id}");
        Assert.Equal(HttpStatusCode.NotFound, stolen.StatusCode);

        var stories = await owner.GetFromJsonAsync<StudioStoriesResponse>("/api/studio/stories");
        Assert.NotNull(stories);
        Assert.Contains(stories.LatestReceipts, c => c.ReceiptId == seeded.ReceiptId);
        Assert.Contains(stories.YouVsPundits, c => c.ReceiptId == seeded.ReceiptId);
        Assert.Contains(stories.PreviousProjects, c => c.ProjectId == body.Pack.Id);
    }

    [Fact]
    public async Task Trending_pack_does_not_invent_a_scoreline()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var item = new NewsFeedItem
        {
            Id = $"studio-{Guid.NewGuid():N}"[..20],
            Title = "Slot talks Anfield",
            Summary = "Sourced quotes from the press conference only.",
            Source = "BBC Sport",
            Url = "https://example.com/slot",
            PublishedAt = DateTimeOffset.UtcNow,
            QualityScore = 90
        };
        db.NewsFeedItems.Add(item);
        await db.SaveChangesAsync();

        var client = await _factory.CreateConsentedAnonymousClientAsync();
        var generated = await client.PostAsJsonAsync("/api/studio/packs", new
        {
            contentType = "caption",
            tone = "explainer",
            feedItemId = item.Id
        });
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);
        var body = await generated.Content.ReadFromJsonAsync<StudioPackGenerateResponse>();
        Assert.NotNull(body?.Pack);
        Assert.DoesNotContain(body.Pack.Facts, f => f.Label == "Result");
        Assert.Contains(body.Pack.Facts, f => f.Label == "Headline" && f.Value == item.Title);
        Assert.Contains(body.Pack.SourceNotes, n => n.Contains(item.Url, StringComparison.Ordinal));
        Assert.DoesNotContain("2–0", body.Pack.Script, StringComparison.Ordinal);
    }

    private async Task<SeededReceipt> SeedReceiptAsync(
        HttpClient owner,
        int? home,
        int? away,
        bool pundit)
    {
        var session = await owner.GetFromJsonAsync<OwnerSession>("/api/auth/session");
        Assert.NotNull(session?.AnonymousUserId);
        await CsrfTestHelper.ApplyCsrfAsync(owner);
        var receiptId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var match = new Match
        {
            Id = $"fd-st-{Guid.NewGuid():N}"[..22],
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            TeamACode = "ARS",
            TeamBCode = "CHE",
            KickoffTime = DateTimeOffset.UtcNow.AddHours(-3),
            Status = "FT",
            HomeScore = home,
            AwayScore = away,
            Stage = "League",
            Venue = "Emirates"
        };
        db.Matches.Add(match);
        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            AnonymousUserId = Guid.Parse(session.AnonymousUserId),
            MatchId = match.Id,
            PredictionType = PredictionType.Result,
            PredictionValue = "home",
            PointsAwarded = 3,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Predictions.Add(prediction);
        db.PredictionReceipts.Add(new PredictionReceipt
        {
            Id = receiptId,
            PredictionId = prediction.Id,
            MatchId = match.Id,
            AnonymousUserId = prediction.AnonymousUserId,
            ResultHash = $"FT:{home}-{away}",
            PredictionType = PredictionType.Result,
            PredictionValue = "home",
            PointsAwarded = 3,
            AuraDelta = 3,
            HomeScore = home,
            AwayScore = away,
            MatchStatus = "FT",
            StoryType = pundit ? ReceiptStoryTypes.BeatPundit : ReceiptStoryTypes.Hit,
            StoryTypesJson = pundit ? "[\"beat_pundit\",\"hit\"]" : "[\"hit\"]",
            PunditTakesJson = pundit
                ? """[{"punditId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Gary Neville","prediction":"Chelsea to win","sourceUrl":"https://example.com/neville","sourcePlatform":"Sky Sports","wasCorrect":false}]"""
                : "[]",
            IsPublic = false,
            SettledAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return new SeededReceipt(receiptId, match.Id);
    }

    private sealed record OwnerSession(string? AnonymousUserId);
    private sealed record SeededReceipt(Guid ReceiptId, string MatchId);
}
