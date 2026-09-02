namespace BanterApp.Api.Data.Entities;

/// <summary>
/// GIF identity shown during a Friday–Monday (or midweek) uniqueness window.
/// </summary>
public class ReactionGifUse
{
    public string WindowId { get; set; } = string.Empty;

    public string GifId { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int? Seed { get; set; }

    public DateTimeOffset UsedAt { get; set; }
}
