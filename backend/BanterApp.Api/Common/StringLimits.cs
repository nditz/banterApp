namespace BanterApp.Api.Common;

public static class StringLimits
{
    public const int ExternalId = 128;
    public const int ErrorMessage = 2000;
    public const int SyncErrorMessage = 4000;
    public const int ApplicationErrorMessage = 1000;
    public const int ApplicationErrorDetail = 8000;
    public const int LeagueName = 25;
    public const int LeagueMemberDisplayName = 100;

    public static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
