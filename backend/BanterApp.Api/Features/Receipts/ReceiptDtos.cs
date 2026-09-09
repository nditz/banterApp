using System.Text.Json;
using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Receipts;

public sealed record ReceiptMatchSummary(
    string Id,
    string TeamA,
    string TeamB,
    string TeamACode,
    string TeamBCode,
    string Status,
    int? HomeScore,
    int? AwayScore,
    DateTimeOffset KickoffTime);

public sealed record ReceiptPunditTakeDto(
    Guid PunditId,
    string Name,
    string Prediction,
    string? SourceUrl,
    string? SourcePlatform,
    bool WasCorrect);

public sealed record ReceiptStoryCandidateDto(
    string StoryType,
    int Rank,
    string Summary);

public sealed record ReceiptDto(
    Guid Id,
    Guid PredictionId,
    string MatchId,
    string PredictionType,
    string PredictionValue,
    int PointsAwarded,
    int AuraDelta,
    int? HomeScore,
    int? AwayScore,
    string MatchStatus,
    string StoryType,
    IReadOnlyList<string> StoryTypes,
    IReadOnlyList<ReceiptPunditTakeDto> PunditTakes,
    IReadOnlyList<ReceiptStoryCandidateDto> StoryCandidates,
    bool IsPublic,
    DateTimeOffset SettledAt,
    DateTimeOffset CreatedAt,
    ReceiptMatchSummary? Match);

public static class ReceiptMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static ReceiptDto Map(PredictionReceipt receipt)
    {
        var storyTypes = Deserialize<List<string>>(receipt.StoryTypesJson) ?? [];
        var takes = Deserialize<List<ReceiptPunditTakeDto>>(receipt.PunditTakesJson) ?? [];
        var candidates = receipt.StoryCandidates
            .OrderBy(c => c.Rank)
            .Select(c => new ReceiptStoryCandidateDto(c.StoryType, c.Rank, c.Summary))
            .ToList();

        ReceiptMatchSummary? match = receipt.Match is null
            ? null
            :             new ReceiptMatchSummary(
                receipt.Match.Id,
                receipt.Match.TeamA,
                receipt.Match.TeamB,
                receipt.Match.TeamACode,
                receipt.Match.TeamBCode,
                receipt.Match.Status,
                receipt.Match.HomeScore,
                receipt.Match.AwayScore,
                receipt.Match.KickoffTime);

        return new ReceiptDto(
            receipt.Id,
            receipt.PredictionId,
            receipt.MatchId,
            ToTypeString(receipt.PredictionType),
            receipt.PredictionValue,
            receipt.PointsAwarded,
            receipt.AuraDelta,
            receipt.HomeScore,
            receipt.AwayScore,
            receipt.MatchStatus,
            receipt.StoryType,
            storyTypes,
            takes,
            candidates,
            receipt.IsPublic,
            receipt.SettledAt,
            receipt.CreatedAt,
            match);
    }

    private static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static string ToTypeString(PredictionType type) =>
        type switch
        {
            PredictionType.CorrectScore => "correct_score",
            PredictionType.DoubleChance => "double_chance",
            _ => "result"
        };
}
