using BanterApp.Api.Integrations.Pundits;
using Xunit;

namespace BanterApp.Api.Tests;

public class StubPunditOpinionExtractorTests
{
    [Fact]
    public async Task ExtractAsync_DoesNotFabricateOpinions()
    {
        var extractor = new StubPunditOpinionExtractor();
        var result = await extractor.ExtractAsync(
            "youtube",
            "Sky Sports",
            "https://youtube.com/watch?v=abc",
            "Preview",
            DateTimeOffset.UtcNow,
            "Host",
            new string('x', 400));

        Assert.Null(result);
    }
}
