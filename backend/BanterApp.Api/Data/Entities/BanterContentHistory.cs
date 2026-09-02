using BanterApp.Api.Common;

namespace BanterApp.Api.Data.Entities;

/// <summary>
/// Additive history of selected banter media for anti-repetition (Strategy Engine).
/// </summary>
public class BanterContentHistory
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? MatchId { get; set; }

    public string? TeamId { get; set; }

    public Guid? PredictionId { get; set; }

    public string ScenarioType { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string? ProviderContentId { get; set; }

    public string? SearchPhrase { get; set; }

    public string? MemeTemplateId { get; set; }

    public string? CaptionHash { get; set; }

    public decimal? SelectionScore { get; set; }

    public DateTimeOffset UsedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public static class BanterContentHistoryLimits
{
    public const int ScenarioType = 64;
    public const int ContentType = 32;
    public const int Provider = 32;
    public const int ProviderContentId = StringLimits.ReactionGifId;
    public const int SearchPhrase = 120;
    public const int MemeTemplateId = 128;
    public const int CaptionHash = StringLimits.ContentHash;
    public const int MatchId = 64;
    public const int TeamId = 64;
}
