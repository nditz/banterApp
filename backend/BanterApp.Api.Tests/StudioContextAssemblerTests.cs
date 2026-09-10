using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Receipts;
using BanterApp.Api.Features.Studio;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class StudioContextAssemblerTests
{
    [Fact]
    public void FromReceipt_uses_recorded_score_and_sourced_pundit_take()
    {
        var receipt = CreateReceipt(home: 2, away: 0, includePundit: true);
        var context = StudioContextAssembler.FromReceipt(receipt);

        Assert.Equal("Arsenal 2–0 Chelsea", context.Scoreline);
        Assert.Contains(context.Facts, f => f.Label == "Result" && f.Value.Contains("2–0", StringComparison.Ordinal));
        Assert.Contains(context.Facts, f => f.Provenance == "pundit_source" && f.Value.Contains("Chelsea to win"));
        Assert.Contains(context.SourceNotes, n => n.Contains("https://example.com/neville", StringComparison.Ordinal));
        Assert.DoesNotContain(context.Facts, f => f.Value.Contains("invent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromReceipt_does_not_invent_a_scoreline_when_scores_are_missing()
    {
        var receipt = CreateReceipt(home: null, away: null, includePundit: false);
        var context = StudioContextAssembler.FromReceipt(receipt);

        Assert.Null(context.Scoreline);
        Assert.DoesNotContain(context.Facts, f => f.Label == "Result");
        Assert.Contains(context.SourceNotes, n => n.Contains("did not invent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromNews_copies_headline_and_source_without_a_scoreboard()
    {
        var item = new NewsFeedItem
        {
            Id = "news-1",
            Title = "Arteta on the derby",
            Summary = "Sourced desk notes from the presser.",
            Source = "Sky Sports",
            Url = "https://example.com/arteta",
            PublishedAt = DateTimeOffset.UtcNow
        };

        var context = StudioContextAssembler.FromNews(item);

        Assert.Equal(StudioContentCatalog.KindTrending, context.StoryKind);
        Assert.Null(context.Scoreline);
        Assert.Contains(context.Facts, f => f.Label == "Headline" && f.Value == item.Title);
        Assert.Contains(context.SourceNotes, n => n.Contains(item.Url, StringComparison.Ordinal));
        Assert.Contains(context.SourceNotes, n => n.Contains("did not invent a scoreline", StringComparison.OrdinalIgnoreCase));
    }

    private static ReceiptDto CreateReceipt(int? home, int? away, bool includePundit) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "fd-test",
            "result",
            "home",
            home.HasValue && home > away ? 3 : 0,
            home.HasValue && home > away ? 3 : 0,
            home,
            away,
            "FT",
            includePundit ? ReceiptStoryTypes.BeatPundit : ReceiptStoryTypes.Hit,
            includePundit ? [ReceiptStoryTypes.BeatPundit, ReceiptStoryTypes.Hit] : [ReceiptStoryTypes.Hit],
            includePundit
                ?
                [
                    new ReceiptPunditTakeDto(
                        Guid.NewGuid(),
                        "Gary Neville",
                        "Chelsea to win",
                        "https://example.com/neville",
                        "Sky Sports",
                        false)
                ]
                : [],
            [
                new ReceiptStoryCandidateDto(
                    includePundit ? ReceiptStoryTypes.BeatPundit : ReceiptStoryTypes.Hit,
                    1,
                    home.HasValue ? "You beat a sourced pundit pick. Arsenal 2–0 Chelsea." : "Your pick scored 0 pts.")
            ],
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            new ReceiptMatchSummary(
                "fd-test",
                "Arsenal",
                "Chelsea",
                "ARS",
                "CHE",
                "FT",
                home,
                away,
                DateTimeOffset.UtcNow.AddHours(-3)));
}
