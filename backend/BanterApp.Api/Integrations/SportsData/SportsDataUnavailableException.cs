namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Raised when the configured live sports provider cannot return fixtures/standings.
/// Jobs must fail visibly rather than substituting mock data.
/// </summary>
public sealed class SportsDataUnavailableException : Exception
{
    public SportsDataUnavailableException(string message) : base(message)
    {
    }

    public SportsDataUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
