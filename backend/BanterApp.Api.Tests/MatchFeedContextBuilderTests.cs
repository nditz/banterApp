using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class MatchFeedContextBuilderTests
{
    [Fact]
    public async Task BuildPunditContextAsync_uses_match_id_not_free_text()
    {
        await using var db = CreateDb();
        var pundit = new Pundit
        {
            Id = Guid.NewGuid(),
            Kind = PunditKind.Source,
            Name = "Gary Neville",
            NormalizedName = "gary neville",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "Sky Sports",
            SourceType = "website",
            ExtractPredictions = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var item = new MediaItem
        {
            Id = Guid.NewGuid(),
            MediaSourceId = source.Id,
            ExternalId = "1",
            Title = "Preview",
            SourceUrl = "https://example.com",
            ProcessingStatus = MediaItemProcessingStatus.Extracted,
            PublishedAt = DateTimeOffset.UtcNow
        };
        db.Pundits.Add(pundit);
        db.MediaSources.Add(source);
        db.MediaItems.Add(item);
        db.PunditOpinions.Add(new PunditOpinion
        {
            Id = Guid.NewGuid(),
            SourceItemId = item.Id,
            PunditId = pundit.Id,
            MatchId = "wc-usa-mex",
            MatchName = "United States vs Mexico",
            Prediction = "United States to win",
            Opinion = "USA look good.",
            ReviewStatus = "approved",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var context = await MatchFeedContextBuilder.BuildPunditContextAsync(db, "wc-usa-mex");

        Assert.NotNull(context);
        Assert.Contains("Gary Neville", context);
        Assert.Contains("United States to win", context);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
