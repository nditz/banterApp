using BanterApp.Api.Integrations.Pundits;
using Xunit;

namespace BanterApp.Api.Tests;

public class SourceTextQualityTests
{
    [Fact]
    public void IsUsable_RejectsShortAndBlank()
    {
        Assert.False(SourceTextQuality.IsUsable(null, 200));
        Assert.False(SourceTextQuality.IsUsable("   ", 200));
        Assert.False(SourceTextQuality.IsUsable(new string('a', 199), 200));
        Assert.True(SourceTextQuality.IsUsable(new string('a', 200), 200));
    }

    [Fact]
    public void IsTitleDescriptionFallback_DetectsConcatenatedMetadata()
    {
        Assert.True(SourceTextQuality.IsTitleDescriptionFallback(
            "Preview show",
            "Gary talks fixtures",
            "Preview show\n\nGary talks fixtures"));
        Assert.False(SourceTextQuality.IsTitleDescriptionFallback(
            "Preview show",
            "Gary talks fixtures",
            new string('x', 400)));
    }
}
