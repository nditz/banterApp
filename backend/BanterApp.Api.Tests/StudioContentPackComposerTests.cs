using BanterApp.Api.Features.Receipts;
using BanterApp.Api.Features.Studio;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class StudioContentPackComposerTests
{
    [Fact]
    public void Compose_keeps_sourced_facts_and_does_not_invent_a_score()
    {
        var context = StudioContextAssembler.FromReceipt(CreateReceipt(home: null, away: null));
        var pack = StudioContentPackComposer.Compose(
            context, "short", "analytical", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal("short", pack.ContentType);
        Assert.Equal("analytical", pack.Tone);
        Assert.Equal(context.Facts, pack.Facts);
        Assert.DoesNotContain("2–0", pack.Script, StringComparison.Ordinal);
        Assert.DoesNotContain("2-0", pack.Script, StringComparison.Ordinal);
        Assert.Contains("No recorded scoreline", pack.Script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(pack.SourceNotes, n => n.Contains("generated framing", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pack.Hashtags, t => t == "#BallTakes");
        Assert.False(string.IsNullOrWhiteSpace(pack.ImagePrompt));
        Assert.False(string.IsNullOrWhiteSpace(pack.VoiceoverPrompt));
        Assert.DoesNotContain("invented quote", pack.Script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_includes_recorded_score_and_sourced_pundit_when_present()
    {
        var context = StudioContextAssembler.FromReceipt(CreateReceipt(home: 2, away: 0, pundit: true));
        var pack = StudioContentPackComposer.Compose(
            context, "thread", "ruthless", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Contains("Arsenal 2–0 Chelsea", pack.Script, StringComparison.Ordinal);
        Assert.Contains("Gary Neville", pack.Script, StringComparison.Ordinal);
        Assert.Contains("Chelsea to win", pack.Script, StringComparison.Ordinal);
        Assert.DoesNotContain("I told you they'd bottle it", pack.Script, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("thread", pack.ContentType);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("podcast")]
    [InlineData("meme")]
    [InlineData("caption")]
    [InlineData("thread")]
    [InlineData("carousel")]
    [InlineData("commentary")]
    public void Compose_supports_each_content_type(string contentType)
    {
        var context = StudioContextAssembler.FromReceipt(CreateReceipt(home: 1, away: 1));
        var pack = StudioContentPackComposer.Compose(
            context, contentType, "funny", Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(contentType, pack.ContentType);
        Assert.False(string.IsNullOrWhiteSpace(pack.Title));
        Assert.False(string.IsNullOrWhiteSpace(pack.Hook));
        Assert.False(string.IsNullOrWhiteSpace(pack.Script));
        Assert.False(string.IsNullOrWhiteSpace(pack.Caption));
        Assert.NotEmpty(pack.VisualPlan);
        Assert.Equal(context.Facts.Count, pack.Facts.Count);
    }

    private static ReceiptDto CreateReceipt(int? home, int? away, bool pundit = false) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "fd-pack",
            "result",
            "home",
            0,
            0,
            home,
            away,
            "FT",
            pundit ? ReceiptStoryTypes.BeatPundit : ReceiptStoryTypes.Miss,
            pundit ? [ReceiptStoryTypes.BeatPundit] : [ReceiptStoryTypes.Miss],
            pundit
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
            [],
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            new ReceiptMatchSummary(
                "fd-pack",
                "Arsenal",
                "Chelsea",
                "ARS",
                "CHE",
                "FT",
                home,
                away,
                DateTimeOffset.UtcNow));
}
