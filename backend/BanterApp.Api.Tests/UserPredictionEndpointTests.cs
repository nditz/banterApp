using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests;

public class UserPredictionEndpointTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public UserPredictionEndpointTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    private static readonly Guid ValidUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid DuplicateUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid InvalidPlayerUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid LockedUserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    [Fact]
    public async Task GetFootballCountries_ReturnsOkWithoutAuth()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/football/countries");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserPredictions_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/user/predictions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrediction_WithInvalidPlayer_ReturnsBadRequest()
    {
        await EnsureUserAsync(InvalidPlayerUserId, "invalid@test.com");
        using var client = _factory.CreateAuthenticatedClient("invalid@test.com", InvalidPlayerUserId);
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsJsonAsync("/api/user/predictions", new
        {
            predictionType = "top_goal_scorer",
            playerId = Guid.NewGuid(),
            turnstileToken = "dev-bypass"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrediction_WithValidPlayer_Succeeds()
    {
        await EnsureUserAsync(ValidUserId, "valid@test.com");
        var playerId = await SeedPlayerAsync();

        using var client = _factory.CreateAuthenticatedClient("valid@test.com", ValidUserId);
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsJsonAsync("/api/user/predictions", new
        {
            predictionType = "golden_boot",
            playerId,
            turnstileToken = "dev-bypass"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateDuplicatePrediction_ReturnsConflict()
    {
        await EnsureUserAsync(DuplicateUserId, "dup@test.com");
        var playerId = await SeedPlayerAsync();

        using var client = _factory.CreateAuthenticatedClient("dup@test.com", DuplicateUserId);
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var payload = new
        {
            predictionType = "best_young_player",
            playerId,
            turnstileToken = "dev-bypass"
        };

        var first = await client.PostAsJsonAsync("/api/user/predictions", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/user/predictions", payload);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task UpdateLockedPrediction_ReturnsBadRequest()
    {
        await EnsureUserAsync(LockedUserId, "locked@test.com");
        var playerId = await SeedPlayerAsync();
        var predictionId = await SeedLockedPredictionAsync(LockedUserId, playerId);

        using var client = _factory.CreateAuthenticatedClient("locked@test.com", LockedUserId);
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PutAsJsonAsync($"/api/user/predictions/{predictionId}", new
        {
            playerId,
            turnstileToken = "dev-bypass"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task EnsureUserAsync(Guid userId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.Users.AnyAsync(u => u.Id == userId))
        {
            return;
        }

        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            DisplayName = "Test User"
        });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedPlayerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var country = new Country
        {
            Id = Guid.NewGuid(),
            Name = "France",
            Code = "FR",
            IsActive = true
        };
        var player = new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test Player",
            CountryId = country.Id,
            IsActive = true
        };
        db.Countries.Add(country);
        db.Players.Add(player);
        await db.SaveChangesAsync();
        return player.Id;
    }

    private async Task<Guid> SeedLockedPredictionAsync(Guid userId, Guid playerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            db.Users.Add(new User
            {
                Id = userId,
                Email = "locked@test.com",
                DisplayName = "Locked User"
            });
        }

        var prediction = new UserPrediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PredictionType = UserPredictionTypes.TopGoalScorer,
            PlayerId = playerId,
            Competition = "WC",
            Season = "2026",
            IsLocked = true,
            LockedAt = DateTimeOffset.UtcNow
        };
        db.UserPredictions.Add(prediction);
        await db.SaveChangesAsync();
        return prediction.Id;
    }
}
