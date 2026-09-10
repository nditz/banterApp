namespace BanterApp.Api.Data.Entities;

/// <summary>
/// Live override for an AI prompt. Empty table means shipped <c>AiOptions</c> defaults are used.
/// </summary>
public class PromptOverride
{
    public string Key { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? UpdatedByUserId { get; set; }
}

public static class PromptOverrideLimits
{
    public const int Key = 80;
    public const int Body = 32_000;
}
