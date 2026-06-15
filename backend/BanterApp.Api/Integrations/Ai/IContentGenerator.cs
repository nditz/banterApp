using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Integrations.Ai;

public enum BanterTone
{
    Friendly,
    Roast,
    Praise
}

public enum VideoScriptFormat
{
    TikTok,
    YouTubeShort,
    Instagram
}

public enum VideoScriptDuration
{
    Fifteen = 15,
    Thirty = 30,
    Sixty = 60
}

public interface IContentGenerator
{
    Task<bool> CanGenerateAsync(
        string? userId,
        bool isAnonymous,
        CancellationToken cancellationToken = default);

    Task<string> GenerateBanterAsync(
        string userPrediction,
        string actualResult,
        BanterTone tone = BanterTone.Friendly,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default);

    Task<string> GenerateAnalysisAsync(
        string userPrediction,
        MatchStatisticsDto matchStats,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default);

    Task<string> GenerateMemeCaptionAsync(
        string context,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default);

    Task<string> GenerateVideoScriptAsync(
        VideoScriptFormat format,
        VideoScriptDuration duration,
        string context,
        string? userId = null,
        bool isAnonymous = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Human-like pundit reaction to a news headline or match update for the rolling feed.
    /// </summary>
    Task<string> GenerateNewsReactionAsync(
        string headline,
        string summary,
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// DALL-E image URL for a feed reaction or meme (ephemeral OpenAI URL). Null when disabled or unavailable.
    /// </summary>
    Task<string?> GenerateReactionImageUrlAsync(
        string headline,
        string reactionText,
        string? category = null,
        CancellationToken cancellationToken = default);
}
