using System.Text.Json;
using BanterApp.Api.Integrations.Pundits.Dtos;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class StubPunditOpinionExtractor : IPunditOpinionExtractor
{
    public Task<PunditExtractionResult?> ExtractAsync(
        string sourceType,
        string sourceName,
        string sourceUrl,
        string sourceTitle,
        DateTimeOffset? publishedAt,
        string? author,
        string sourceText,
        CancellationToken cancellationToken = default)
    {
        var punditName = string.IsNullOrWhiteSpace(author) ? "Unknown" : author;
        var quote = Truncate(sourceText, 120) ?? sourceTitle;
        var opinion = new PunditExtractionOpinionDto(
            Topic: "World Cup 2026",
            Team: InferTeam(sourceText),
            Player: null,
            Match: null,
            MatchId: null,
            Opinion: $"Stub summary of take from {sourceName}.",
            Prediction: InferPrediction(sourceText),
            PredictionType: "general_opinion",
            Confidence: 0.55,
            EvidenceQuote: quote,
            QuoteContext: "Opening section of source text.",
            IsDirectQuote: quote.Length > 20 && sourceText.Contains(quote, StringComparison.OrdinalIgnoreCase),
            NeedsHumanReview: punditName == "Unknown");

        var result = new PunditExtractionResult(
            sourceType,
            sourceName,
            sourceUrl,
            sourceTitle,
            publishedAt,
            [
                new PunditExtractionPunditDto(
                    punditName,
                    "pundit",
                    [opinion])
            ],
            sourceText.Length < 200 ? ["Source text may be incomplete."] : [],
            $"Stub extraction summary for {sourceTitle}.",
            "{}");

        return Task.FromResult<PunditExtractionResult?>(result);
    }

    private static string? InferTeam(string text)
    {
        string[] teams = ["England", "Brazil", "France", "Argentina", "Germany", "Spain"];
        return teams.FirstOrDefault(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private static string? InferPrediction(string text)
    {
        if (text.Contains("win", StringComparison.OrdinalIgnoreCase))
        {
            return "Predicted a tournament winner from source context.";
        }

        return null;
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= max)
        {
            return value?.Trim();
        }

        return value[..max].Trim();
    }
}
