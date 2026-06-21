using BanterApp.Api.Common;

namespace BanterApp.Api.Integrations.Common;

public static class ProviderErrorMapper
{
    public static ProviderAppException MapOpenAi(
        int statusCode,
        string? operation = null,
        string? model = null,
        string? providerRequestId = null,
        string? rawMessage = null)
    {
        var (safeMessage, retryable) = statusCode switch
        {
            429 => ("AI service is temporarily busy. Please try again shortly.", true),
            408 or 504 => ("AI service timed out. Please try again.", true),
            >= 500 => ("AI service is temporarily unavailable.", true),
            400 => ("AI request could not be processed.", false),
            _ => ("AI service request failed.", statusCode >= 500)
        };

        return new ProviderAppException(
            ErrorCodes.OpenAiApiError,
            safeMessage,
            statusCode >= 500 ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status502BadGateway,
            "openai",
            retryable,
            operation,
            providerRequestId,
            new Dictionary<string, object?>
            {
                ["model"] = model,
                ["status_code"] = statusCode,
                ["raw_message"] = rawMessage
            });
    }

    public static ProviderAppException MapYouTube(
        int statusCode,
        string? operation = null,
        string? videoId = null,
        string? channelId = null,
        string? rawMessage = null)
    {
        var (safeMessage, retryable) = statusCode switch
        {
            403 => ("YouTube access is restricted or quota exceeded.", false),
            404 => ("The requested video was not found.", false),
            429 => ("YouTube rate limit reached. Please try again later.", true),
            408 or 504 => ("YouTube request timed out.", true),
            >= 500 => ("YouTube service is temporarily unavailable.", true),
            _ => ("YouTube request failed.", statusCode >= 500)
        };

        return new ProviderAppException(
            ErrorCodes.YouTubeApiError,
            safeMessage,
            statusCode >= 500 ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status502BadGateway,
            "youtube",
            retryable,
            operation,
            metadata: new Dictionary<string, object?>
            {
                ["video_id"] = videoId,
                ["channel_id"] = channelId,
                ["status_code"] = statusCode,
                ["raw_message"] = rawMessage
            });
    }

    public static ProviderAppException MapRss(
        string reason,
        int? statusCode = null,
        string? feedUrl = null,
        string? articleUrl = null,
        bool ssrfBlocked = false)
    {
        var safeMessage = ssrfBlocked
            ? "Feed URL is not allowed."
            : reason switch
            {
                "timeout" => "Feed request timed out.",
                "invalid_xml" => "Feed returned invalid content.",
                "non_200" => "Feed is currently unavailable.",
                "parse_failed" => "Article content could not be extracted.",
                _ => "We could not load this feed right now."
            };

        var retryable = !ssrfBlocked && reason is "timeout" or "non_200";

        return new ProviderAppException(
            ErrorCodes.RssFetchError,
            safeMessage,
            StatusCodes.Status502BadGateway,
            "rss",
            retryable,
            "fetch",
            metadata: new Dictionary<string, object?>
            {
                ["feed_url"] = feedUrl,
                ["article_url"] = articleUrl,
                ["status_code"] = statusCode,
                ["reason"] = reason,
                ["ssrf_blocked"] = ssrfBlocked
            });
    }

    public static int ComputeRetryDelaySeconds(int retryCount) =>
        Math.Min(3600, 30 * (int)Math.Pow(2, Math.Min(retryCount, 10)));
}
