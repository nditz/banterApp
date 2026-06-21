using System.Net;

namespace BanterApp.Api.Services;

public interface ISafeHttpClient
{
    Task<SafeHttpResponse?> GetStringAsync(string url, CancellationToken ct = default);
}

public sealed record SafeHttpResponse(string Content, string ContentType, HttpStatusCode StatusCode);

public sealed class SafeHttpClient(
    IHttpClientFactory httpClientFactory,
    IOutboundUrlValidator urlValidator,
    ILogger<SafeHttpClient> logger) : ISafeHttpClient
{
    public const int DefaultTimeoutSeconds = 10;
    public const int MaxResponseBytes = 5 * 1024 * 1024;

    public async Task<SafeHttpResponse?> GetStringAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var validation = await urlValidator.ValidateAsync(url, ct);
        if (!validation.IsAllowed)
        {
            logger.LogWarning("SSRF blocked fetch for {Url}: {Reason}", url, validation.Reason);
            return null;
        }

        using var client = httpClientFactory.CreateClient(nameof(SafeHttpClient));
        client.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

        var currentUrl = url;
        for (var redirect = 0; redirect <= 3; redirect++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, currentUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", "BanterApp/1.0 (+https://banter.app)");

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            if (IsRedirect(response.StatusCode) && response.Headers.Location is not null)
            {
                var nextUri = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(new Uri(currentUrl), response.Headers.Location);

                var redirectValidation = await urlValidator.ValidateRedirectAsync(nextUri, url, ct);
                if (!redirectValidation.IsAllowed)
                {
                    logger.LogWarning(
                        "SSRF blocked redirect for {Url} -> {Next}: {Reason}",
                        currentUrl,
                        nextUri,
                        redirectValidation.Reason);
                    return null;
                }

                currentUrl = nextUri.ToString();
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return new SafeHttpResponse(
                    string.Empty,
                    response.Content.Headers.ContentType?.MediaType ?? "text/plain",
                    response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new MemoryStream();
            var buffer = new byte[8192];
            var total = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                total += read;
                if (total > MaxResponseBytes)
                {
                    logger.LogWarning("Response exceeded max size for {Url}.", currentUrl);
                    return null;
                }

                await reader.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";
            if (!IsAllowedContentType(contentType))
            {
                logger.LogWarning("Blocked content type {ContentType} for {Url}.", contentType, currentUrl);
                return null;
            }

            reader.Position = 0;
            using var textReader = new StreamReader(reader);
            var body = await textReader.ReadToEndAsync(ct);
            return new SafeHttpResponse(body, contentType, response.StatusCode);
        }

        logger.LogWarning("Too many redirects for {Url}.", url);
        return null;
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private static bool IsAllowedContentType(string contentType) =>
        contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("html", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("rss", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("atom", StringComparison.OrdinalIgnoreCase);
}
