using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public class UnsourcedPunditPurgeTests
{
    [Fact]
    public async Task RemovesStubWorldCupAndUnmatchedPundits_KeepsPremierLeagueLinked()
    {
        await using var db = TestDbContextFactory.Create();
        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "Sky Sports",
            SourceType = "website",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var plItem = new MediaItem
        {
            Id = Guid.NewGuid(),
            MediaSourceId = source.Id,
            MediaSource = source,
            ExternalId = "pl-article",
            Title = "Arsenal vs Chelsea preview",
            SourceUrl = "https://www.skysports.com/football/arsenal-chelsea-preview",
            Description = "Premier League preview notes",
            RawText = new string('x', 400),
            ProcessingStatus = MediaItemProcessingStatus.Extracted
        };
        var junkItem = new MediaItem
        {
            Id = Guid.NewGuid(),
            MediaSourceId = source.Id,
            MediaSource = source,
            ExternalId = "wc-article",
            Title = "World Cup hydration breaks",
            SourceUrl = "https://www.theguardian.com/football/2026/jun/20/world-cup",
            Description = "World Cup notes",
            RawText = "World Cup hydration breaks\n\nWorld Cup notes",
            ProcessingStatus = MediaItemProcessingStatus.Extracted
        };
        var plMatch = new Match
        {
            Id = "fd-1001",
            TeamA = "Arsenal",
            TeamB = "Chelsea",
            TeamACode = "ARS",
            TeamBCode = "CHE",
            KickoffTime = DateTimeOffset.UtcNow.AddDays(2),
            Stage = "Regular Season - 3",
            Group = "PL",
            Venue = "Emirates",
            Status = "NS"
        };
        var keepPundit = new Pundit
        {
            Id = Guid.NewGuid(),
            Kind = PunditKind.Source,
            Name = "Gary Neville",
            NormalizedName = "gary neville",
            Organization = "Sky Sports"
        };
        var junkPundit = new Pundit
        {
            Id = Guid.NewGuid(),
            Kind = PunditKind.Source,
            Name = "Aidan O'Brien",
            NormalizedName = "aidan o'brien",
            Organization = "Sky Sports"
        };
        var keepOpinion = new PunditOpinion
        {
            Id = Guid.NewGuid(),
            SourceItemId = plItem.Id,
            SourceItem = plItem,
            PunditId = keepPundit.Id,
            Pundit = keepPundit,
            MatchId = plMatch.Id,
            Match = plMatch,
            Opinion = "Arsenal to win at home.",
            ExtractedJson = "{\"ok\":true}"
        };
        var stubOpinion = new PunditOpinion
        {
            Id = Guid.NewGuid(),
            SourceItemId = junkItem.Id,
            SourceItem = junkItem,
            PunditId = junkPundit.Id,
            Pundit = junkPundit,
            Opinion = "Stub summary of take from Sky Sports.",
            ExtractedJson = "{}"
        };

        db.MediaSources.Add(source);
        db.MediaItems.AddRange(plItem, junkItem);
        db.Matches.Add(plMatch);
        db.Pundits.AddRange(keepPundit, junkPundit);
        db.PunditOpinions.AddRange(keepOpinion, stubOpinion);
        db.NewsFeedItems.Add(new NewsFeedItem
        {
            Id = PunditOpinionFeedMapper.FeedItemId(stubOpinion.Id),
            Source = "Sky Sports",
            Title = "Stub take",
            Url = junkItem.SourceUrl,
            Category = PunditOpinionFeedMapper.FeedCategory,
            PublishedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var removed = await UnsourcedPunditPurge.ExecuteAsync(db);

        Assert.True(removed > 0);
        Assert.False(await db.Pundits.AnyAsync(p => p.Name == "Aidan O'Brien"));
        Assert.True(await db.Pundits.AnyAsync(p => p.Name == "Gary Neville"));
        Assert.False(await db.PunditOpinions.AnyAsync(o => o.Opinion.StartsWith("Stub summary")));
        Assert.False(await db.NewsFeedItems.AnyAsync(n => n.Title == "Stub take"));
    }
}
