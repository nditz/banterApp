using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests;

public class MatchPredictionEndpointTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public MatchPredictionEndpointTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_OnUpcomingMatch_PersistsAndRejectsDuplicate()
    {
        var matchId = await SeedMatchAsync("NS", DateTimeOffset.UtcNow.AddDays(2));
        using var client = await _factory.CreateConsentedAnonymousClientAsync();

        var created = await client.PostAsJsonAsync("/api/predictions/create", new
        {
            matchId,
            predictionType = "result",
            predictionValue = "H",
            turnstileToken = "dev-bypass"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/predictions/create", new
        {
            matchId,
            predictionType = "result",
            predictionValue = "A",
            turnstileToken = "dev-bypass"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var history = await client.GetAsync("/api/predictions/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        var body = await history.Content.ReadAsStringAsync();
        Assert.Contains(matchId, body, StringComparison.Ordinal);
        Assert.Contains("\"predictionValue\":\"H\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_OnFinishedMatch_ReturnsBadRequest()
    {
        var matchId = await SeedMatchAsync("FT", DateTimeOffset.UtcNow.AddHours(-2));
        using var client = await _factory.CreateConsentedAnonymousClientAsync();

        var response = await client.PostAsJsonAsync("/api/predictions/create", new
        {
            matchId,
            predictionType = "result",
            predictionValue = "H",
            turnstileToken = "dev-bypass"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("locked", body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> SeedMatchAsync(string status, DateTimeOffset kickoff)
    {
        var matchId = $"pred-{Guid.NewGuid():N}";
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Matches.Add(new Match
        {
            Id = matchId,
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            TeamACode = "ARS",
            TeamBCode = "CHE",
            KickoffTime = kickoff,
            Status = status,
            Stage = "Premier League",
            Group = "PL",
            Venue = "Emirates",
            MatchweekNumber = 3,
            HomeScore = status == "FT" ? 1 : null,
            AwayScore = status == "FT" ? 0 : null
        });
        await db.SaveChangesAsync();
        return matchId;
    }
}
