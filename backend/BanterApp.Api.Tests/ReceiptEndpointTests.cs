using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Receipts;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class ReceiptEndpointTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public ReceiptEndpointTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task List_requires_session_and_does_not_leak_other_owners()
    {
        var ownerClient = await _factory.CreateConsentedAnonymousClientAsync();
        var stranger = await _factory.CreateConsentedAnonymousClientAsync();

        Guid receiptId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = await ownerClient.GetFromJsonAsync<OwnerSession>("/api/auth/session");
            Assert.NotNull(session?.AnonymousUserId);
            var match = new Match
            {
                Id = $"fd-ep-{Guid.NewGuid():N}"[..22],
                TeamA = "Arsenal",
                TeamB = "Chelsea",
                TeamACode = "ARS",
                TeamBCode = "CHE",
                KickoffTime = DateTimeOffset.UtcNow.AddHours(-3),
                Status = "FT",
                HomeScore = 2,
                AwayScore = 0,
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
            var receipt = new PredictionReceipt
            {
                Id = Guid.NewGuid(),
                PredictionId = prediction.Id,
                MatchId = match.Id,
                AnonymousUserId = prediction.AnonymousUserId,
                ResultHash = "FT:2-0",
                PredictionType = PredictionType.Result,
                PredictionValue = "home",
                PointsAwarded = 3,
                AuraDelta = 3,
                HomeScore = 2,
                AwayScore = 0,
                MatchStatus = "FT",
                StoryType = ReceiptStoryTypes.Hit,
                StoryTypesJson = "[\"hit\"]",
                PunditTakesJson = "[]",
                IsPublic = false,
                SettledAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.PredictionReceipts.Add(receipt);
            await db.SaveChangesAsync();
            receiptId = receipt.Id;
        }

        var guest = _factory.CreateClient();
        var unauth = await guest.GetAsync("/api/receipts");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        var mine = await ownerClient.GetAsync("/api/receipts");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var mineBody = await mine.Content.ReadAsStringAsync();
        Assert.Contains(receiptId.ToString(), mineBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("anonymousUserId", mineBody, StringComparison.OrdinalIgnoreCase);

        var theirs = await stranger.GetAsync("/api/receipts");
        Assert.Equal(HttpStatusCode.OK, theirs.StatusCode);
        var theirsBody = await theirs.Content.ReadAsStringAsync();
        Assert.DoesNotContain(receiptId.ToString(), theirsBody, StringComparison.OrdinalIgnoreCase);

        var stolen = await stranger.GetAsync($"/api/receipts/{receiptId}");
        Assert.Equal(HttpStatusCode.NotFound, stolen.StatusCode);

        var feed = await guest.GetAsync("/api/feed");
        Assert.Equal(HttpStatusCode.OK, feed.StatusCode);
        var feedBody = await feed.Content.ReadAsStringAsync();
        Assert.DoesNotContain(receiptId.ToString(), feedBody, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record OwnerSession(string? AnonymousUserId);
}
