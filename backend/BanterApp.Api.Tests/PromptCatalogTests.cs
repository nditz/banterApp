using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public class PromptCatalogTests
{
    [Fact]
    public async Task ResolveAsync_UsesCodeDefaultWhenTableIsEmpty()
    {
        await using var db = TestDbContextFactory.Create();
        var catalog = CreateCatalog(db, "shipped default");

        var resolved = await catalog.ResolveAsync(PromptKeys.NewsReaction);

        Assert.Contains("shipped default", resolved, StringComparison.Ordinal);
        Assert.Contains("Premier League", resolved, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAsync_UsesDatabaseOverrideWithoutRedeploy()
    {
        await using var db = TestDbContextFactory.Create();
        var catalog = CreateCatalog(db, "shipped default");

        await catalog.SaveAsync(PromptKeys.NewsReaction, "Live override: talk like a Super Sunday desk.", null);

        var resolved = await catalog.ResolveAsync(PromptKeys.NewsReaction);

        Assert.Contains("Live override", resolved, StringComparison.Ordinal);
        Assert.DoesNotContain("shipped default", resolved, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_EmptyBodyClearsOverride()
    {
        await using var db = TestDbContextFactory.Create();
        var catalog = CreateCatalog(db, "shipped default");
        await catalog.SaveAsync(PromptKeys.FeedBanter, "temporary override", null);

        var cleared = await catalog.SaveAsync(PromptKeys.FeedBanter, "  ", null);

        Assert.False(cleared.IsOverride);
        Assert.Equal("shipped default", cleared.Body);
        Assert.Contains("shipped default", await catalog.ResolveAsync(PromptKeys.FeedBanter), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListAsync_IncludesShippedDefaultsForKnownKeys()
    {
        await using var db = TestDbContextFactory.Create();
        var catalog = CreateCatalog(db, "shipped default");

        var list = await catalog.ListAsync();

        Assert.Equal(PromptKeys.All.Count, list.Count);
        Assert.All(list, e => Assert.False(e.IsOverride));
        Assert.Contains(list, e => e.Key == PromptKeys.PunditExtraction);
    }

    private static PromptCatalog CreateCatalog(Data.AppDbContext db, string defaultBody)
    {
        var options = Options.Create(new AiOptions
        {
            NewsReactionSystemPrompt = defaultBody,
            FeedBanterSystemPrompt = defaultBody,
            MemeImagePrompt = defaultBody,
            FeedVisualSystemPrompt = defaultBody,
            PunditExtractionSystemPrompt = defaultBody
        });
        return new PromptCatalog(db, options, new MemoryCache(new MemoryCacheOptions()));
    }
}
