using BanterApp.Api.Integrations.Ai;

namespace BanterApp.Api.Features.Ai;

public sealed record AnalyzeRequest(
    string MatchId,
    string UserPrediction);

public sealed record BanterRequest(
    string UserPrediction,
    string ActualResult,
    BanterTone Tone = BanterTone.Friendly);

public sealed record MemeRequest(string Context);

public sealed record VideoScriptRequest(
    string Context,
    VideoScriptFormat Format = VideoScriptFormat.TikTok,
    VideoScriptDuration Duration = VideoScriptDuration.Thirty);

public sealed record AiGenerationResponse(
    string Content,
    string Type,
    int? RemainingGenerations);
