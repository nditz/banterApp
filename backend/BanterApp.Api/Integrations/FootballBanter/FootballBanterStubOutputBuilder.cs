using System.Text.Json;

namespace BanterApp.Api.Integrations.FootballBanter;

public static class FootballBanterStubOutputBuilder
{
    public static FootballBanterOutput Build(FootballBanterSourceInput input)
    {
        var title = !string.IsNullOrWhiteSpace(input.SourceTitle)
            ? input.SourceTitle.Trim()
            : input.SourceText.Trim();
        if (title.Length > 80)
        {
            title = title[..77] + "…";
        }

        var punditPrefix = string.IsNullOrWhiteSpace(input.PunditName)
            ? string.Empty
            : $"{input.PunditName} said WHAT now? 💀 — ";

        return new FootballBanterOutput
        {
            Headline = $"{punditPrefix}No cap: {title}",
            BanterSummary =
                $"Lowkey this is giving football Twitter energy — {TrimForSummary(input.SourceText)} " +
                $"(via {input.SourceName}; facts stay facts, banter stays banter).",
            MemeReactions =
            [
                "POV: you read this headline and opened the group chat instantly 💀",
                "The timeline is not ready for this take 😭"
            ],
            GifSuggestions = ["football fan shocked reaction", "pundit angry desk"],
            FanReactions = ["NO CAP 😭", "it's giving delulu", "football Twitter wins again"],
            Confidence = input.Confidence ?? 0.75,
            SourceName = input.SourceName,
            SourceUrl = input.SourceUrl,
            PunditName = input.PunditName,
            Prediction = input.Prediction,
            StatementType = input.StatementType ?? FootballBanterStatementType.AiSummary,
            NeedsHumanReview = string.IsNullOrWhiteSpace(input.SourceUrl) ||
                               input.SourceText.Trim().Length < 40
        };
    }

    public static string BuildJson(FootballBanterSourceInput input)
    {
        var output = Build(input);
        return JsonSerializer.Serialize(output, FootballBanterJson.OutputOptions);
    }

    private static string TrimForSummary(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length <= 220 ? trimmed : trimmed[..217] + "…";
    }
}
