using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.UserPredictions;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests;

public class UserPredictionValidatorTests
{
    [Fact]
    public async Task ValidateCreateOrUpdateAsync_RejectsUnknownPredictionType()
    {
        await using var db = TestDbContextFactory.Create();
        var validator = new UserPredictionValidator(db);

        var (isValid, error) = await validator.ValidateCreateOrUpdateAsync(
            "not_a_real_type",
            null,
            null,
            "PL",
            "2026",
            null);

        Assert.False(isValid);
        Assert.Equal("Invalid prediction type.", error);
    }

    [Fact]
    public async Task ValidateCreateOrUpdateAsync_RejectsLockedExistingPrediction()
    {
        await using var db = TestDbContextFactory.Create();
        var validator = new UserPredictionValidator(db);
        var existing = new UserPrediction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PredictionType = UserPredictionTypes.GoldenBoot,
            IsLocked = true,
            LockedAt = DateTimeOffset.UtcNow,
            Competition = "PL",
            Season = "2026"
        };

        var (isValid, error) = await validator.ValidateCreateOrUpdateAsync(
            UserPredictionTypes.GoldenBoot,
            null,
            Guid.NewGuid(),
            "PL",
            "2026",
            existing);

        Assert.False(isValid);
        Assert.Equal("This prediction is locked and cannot be edited.", error);
    }

    [Fact]
    public async Task ValidateCreateOrUpdateAsync_AllowsLeagueWinnerWithoutCountry()
    {
        await using var db = TestDbContextFactory.Create();
        var validator = new UserPredictionValidator(db);

        var (isValid, error) = await validator.ValidateCreateOrUpdateAsync(
            UserPredictionTypes.LeagueWinner,
            null,
            null,
            "PL",
            "2026",
            null);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public async Task ValidateCreateOrUpdateAsync_RejectsInactivePlayerForGoldenBoot()
    {
        await using var db = TestDbContextFactory.Create();
        var playerId = Guid.NewGuid();
        db.Players.Add(new Player
        {
            Id = playerId,
            DisplayName = "Inactive",
            IsActive = false
        });
        await db.SaveChangesAsync();

        var validator = new UserPredictionValidator(db);
        var (isValid, error) = await validator.ValidateCreateOrUpdateAsync(
            UserPredictionTypes.GoldenBoot,
            null,
            playerId,
            "PL",
            "2026",
            null);

        Assert.False(isValid);
        Assert.Equal("That player is not available for selection.", error);
    }

    [Fact]
    public async Task ValidateCreateOrUpdateAsync_RequiresPlayerForGoldenBoot()
    {
        await using var db = TestDbContextFactory.Create();
        var validator = new UserPredictionValidator(db);

        var (isValid, error) = await validator.ValidateCreateOrUpdateAsync(
            UserPredictionTypes.GoldenBoot,
            null,
            null,
            "PL",
            "2026",
            null);

        Assert.False(isValid);
        Assert.Equal("A player is required for this prediction type.", error);
    }

    [Fact]
    public async Task ValidateCreateOrUpdateAsync_AcceptsActivePlayerPrediction()
    {
        await using var db = TestDbContextFactory.Create();
        var countryId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        db.Countries.Add(new Country { Id = countryId, Name = "France", Code = "FR", IsActive = true });
        db.Players.Add(new Player
        {
            Id = playerId,
            DisplayName = "Test Player",
            CountryId = countryId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var validator = new UserPredictionValidator(db);
        var (isValid, error) = await validator.ValidateCreateOrUpdateAsync(
            UserPredictionTypes.BestPlayer,
            null,
            playerId,
            "PL",
            "2026",
            null);

        Assert.True(isValid);
        Assert.Null(error);
    }
}
