using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Matches;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class MatchResolutionServiceTests
{
    [Fact]
    public async Task ResolveAsync_matches_team_names_in_fixture_text()
    {
        await using var db = CreateDb();
        db.Matches.Add(new Match
        {
            Id = "wc-usa-mex",
            TeamA = "United States",
            TeamB = "Mexico",
            TeamACode = "USA",
            TeamBCode = "MEX",
            KickoffTime = DateTimeOffset.UtcNow.AddDays(2),
            Stage = "Group",
            Group = "A",
            Venue = "Azteca"
        });
        await db.SaveChangesAsync();

        var service = new MatchResolutionService(db);
        var result = await service.ResolveAsync("United States vs Mexico", "United States");

        Assert.Equal("wc-usa-mex", result.MatchId);
        Assert.True(result.Confidence >= 0.9);
    }

    [Fact]
    public async Task ResolveAsync_uses_country_aliases()
    {
        await using var db = CreateDb();
        db.Countries.Add(new Country
        {
            Id = Guid.NewGuid(),
            Name = "Brazil",
            Code = "BRA",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.Matches.Add(new Match
        {
            Id = "wc-bra-arg",
            TeamA = "Brazil",
            TeamB = "Argentina",
            TeamACode = "BRA",
            TeamBCode = "ARG",
            KickoffTime = DateTimeOffset.UtcNow.AddDays(1),
            Stage = "Group",
            Group = "D",
            Venue = "Test"
        });
        await db.SaveChangesAsync();

        var service = new MatchResolutionService(db);
        var result = await service.ResolveAsync("Brazil v Argentina", null);

        Assert.Equal("wc-bra-arg", result.MatchId);
    }

    [Fact]
    public void IsMatchLevelPrediction_recognizes_match_types()
    {
        Assert.True(MatchResolutionService.IsMatchLevelPrediction("match_result"));
        Assert.True(MatchResolutionService.IsMatchLevelPrediction("correct_score"));
        Assert.False(MatchResolutionService.IsMatchLevelPrediction("general_opinion"));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
