namespace BanterApp.Api.Common;

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string RateLimited = "RATE_LIMITED";
    public const string CsrfFailed = "CSRF_FAILED";
    public const string BadRequest = "BAD_REQUEST";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string ExternalApiError = "EXTERNAL_API_ERROR";
    public const string OpenAiApiError = "OPENAI_API_ERROR";
    public const string YouTubeApiError = "YOUTUBE_API_ERROR";
    public const string RssFetchError = "RSS_FETCH_ERROR";
    public const string JobFailed = "JOB_FAILED";
    public const string AiOutputValidationError = "AI_OUTPUT_VALIDATION_ERROR";
    public const string ConfigurationError = "CONFIGURATION_ERROR";
    public const string UnknownError = "UNKNOWN_ERROR";
}
