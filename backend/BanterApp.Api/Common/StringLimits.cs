namespace BanterApp.Api.Common;

public static class StringLimits
{
    public const int ExternalId = 128;
    public const int ErrorMessage = 2000;
    public const int SyncErrorMessage = 4000;
    public const int ApplicationErrorMessage = 1000;
    public const int ApplicationErrorDetail = 8000;
    public const int OperationalErrorMessage = 2000;
    public const int OperationalErrorInternal = 4000;
    public const int OperationalErrorStack = 8000;
    public const int OperationalErrorMetadata = 8000;
    public const int LeagueName = 25;
    public const int LeagueMemberDisplayName = 100;
    public const int MediaAuthor = 120;
    public const int MediaPublication = 120;
    public const int ContentHash = 64;
    public const int ProcessingStatus = 16;
    public const int ProcessingError = 500;
    public const int PunditRole = 32;
    public const int PunditNormalizedName = 120;
    public const int PredictionEntityType = 32;
    public const int PredictionEntityName = 120;
    public const int PredictionType = 32;
    public const int OpinionTopic = 120;
    public const int OpinionTeam = 80;
    public const int OpinionPlayer = 120;
    public const int OpinionMatchName = 200;
    public const int RssFeedSlug = 80;
    public const int RssFeedStyleSlug = 64;
    public const int ReactionGifWindowId = 16;
    public const int ReactionGifId = 512;
    public const int ReactionGifUrl = 512;

    public static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
