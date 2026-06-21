namespace BanterApp.Api.Data.Entities;

public static class MediaItemProcessingStatus
{
    public const string Pending = "pending";
    public const string Enriched = "enriched";
    public const string Extracted = "extracted";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
}
