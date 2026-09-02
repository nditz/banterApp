using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Banter;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public class BanterHistoryServiceTests
{
    [Fact]
    public async Task GetExclusions_EmptyHistory_ReturnsNoExclusions()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        var exclusions = await sut.GetExclusionsAsync(Context(userId: Guid.NewGuid()));

        Assert.Empty(exclusions.ProviderContentIds);
        Assert.Empty(exclusions.MemeTemplateIds);
        Assert.Empty(exclusions.SearchPhrases);
    }

    [Fact]
    public async Task GetExclusions_SameGifRecentlyUsedBySameUser_IsExcluded()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.BanterContentHistories.Add(History(userId, "gif-recent", daysAgo: 2));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var exclusions = await sut.GetExclusionsAsync(Context(userId));

        Assert.Contains("gif-recent", exclusions.ProviderContentIds);
    }

    [Fact]
    public async Task GetExclusions_OlderContentOutsideWindow_IsAllowed()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.BanterContentHistories.Add(History(userId, "gif-old", daysAgo: 45));
        await db.SaveChangesAsync();

        var sut = CreateSut(db, userDays: 30, teamDays: 14, globalDays: 3);
        var exclusions = await sut.GetExclusionsAsync(Context(userId));

        Assert.DoesNotContain("gif-old", exclusions.ProviderContentIds);
    }

    [Fact]
    public async Task GetExclusions_SameMemeTemplateRecentlyUsed_IsExcluded()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.BanterContentHistories.Add(new BanterContentHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ScenarioType = "GenericNews",
            ContentType = "gif",
            Provider = "giphy",
            MemeTemplateId = "drake-hotline",
            UsedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var exclusions = await sut.GetExclusionsAsync(Context(userId));

        Assert.Contains("drake-hotline", exclusions.MemeTemplateIds);
    }

    [Fact]
    public async Task GetExclusions_RecentSearchPhrase_IsExcluded()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.BanterContentHistories.Add(new BanterContentHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ScenarioType = "PredictionAgedBadly",
            ContentType = "gif",
            Provider = "giphy",
            ProviderContentId = "gif-x",
            SearchPhrase = "Delete Tweet Reaction",
            UsedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var exclusions = await sut.GetExclusionsAsync(Context(userId));

        Assert.Contains("delete tweet reaction", exclusions.SearchPhrases);
        Assert.True(exclusions.IsSearchPhraseExcluded("Delete Tweet Reaction"));
    }

    [Fact]
    public async Task RecordAsync_PersistsSelectionMetadata()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);
        var userId = Guid.NewGuid();

        await sut.RecordAsync(new BanterSelection(
            Context(userId),
            BanterScenario.PredictionAgedBadly,
            "gif",
            "giphy",
            "abc123",
            "delete tweet reaction",
            null,
            null,
            0.82m,
            "https://media.giphy.com/media/abc123/giphy.gif"));

        var row = Assert.Single(db.BanterContentHistories);
        Assert.Equal(userId, row.UserId);
        Assert.Equal("abc123", row.ProviderContentId);
        Assert.Equal("PredictionAgedBadly", row.ScenarioType);
        Assert.Equal("delete tweet reaction", row.SearchPhrase);
    }

    private static BanterHistoryService CreateSut(
        AppDbContext db,
        int userDays = 30,
        int teamDays = 14,
        int globalDays = 3) =>
        new(
            db,
            Options.Create(new BanterOptions
            {
                RecentContentWindowDays = userDays,
                RecentTeamContentWindowDays = teamDays,
                GlobalHardRepeatWindowDays = globalDays
            }),
            NullLogger<BanterHistoryService>.Instance);

    private static BanterContext Context(Guid? userId = null, string? teamId = null) =>
        new(
            userId,
            PredictionId: null,
            MatchId: null,
            TeamId: teamId,
            TeamName: null,
            OpponentName: null,
            PredictionOutcomeKind.Unknown,
            MatchOutcomeKind.Unknown,
            null,
            null,
            null);

    private static BanterContentHistory History(Guid userId, string gifId, int daysAgo) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ScenarioType = "GenericNews",
            ContentType = "gif",
            Provider = "giphy",
            ProviderContentId = gifId,
            UsedAtUtc = DateTimeOffset.UtcNow.AddDays(-daysAgo)
        };
}
