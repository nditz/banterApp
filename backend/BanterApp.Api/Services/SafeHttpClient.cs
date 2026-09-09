using System.Net;

namespace BanterApp.Api.Services;

public interface ISafeHttpClient
{
    Task<SafeHttpResponse?> GetStringAsync(string url, CancellationToken ct = default);

    Task<SafeHttpFetchResult> FetchAsync(string url, CancellationToken ct = default) =>
        FetchAsync(url, maxResponseBytes: null, ct);

    async Task<SafeHttpFetchResult> FetchAsync(string url, int? maxResponseBytes, CancellationToken ct = default)
    {
        var response = await GetStringAsync(url, ct);
        return response is null
            ? SafeHttpFetchResult.Fail(SafeHttpFailureKind.Unavailable)
            : SafeHttpFetchResult.Ok(response);
    }
}

public enum SafeHttpFailureKind
{
    None,
    EmptyUrl,
    Ssrf,
    HttpStatus,
    ContentType,
    Oversized,
    TooManyRedirects,
    Unavailable
}

public sealed record SafeHttpFetchResult(
    SafeHttpResponse? Response,
    SafeHttpFailureKind FailureKind,
    string? FailureReason = null)
{
    public static SafeHttpFetchResult Ok(SafeHttpResponse response) =>
        new(response, SafeHttpFailureKind.None);

    public static SafeHttpFetchResult Fail(SafeHttpFailureKind kind, string? reason = null) =>
        new(null, kind, reason);
}

public sealed record SafeHttpResponse(
    string Content,
    string ContentType,
    HttpStatusCode StatusCode,
    string? FinalUrl = null);

public sealed class SafeHttpClient(
    IHttpClientFactory httpClientFactory,
    IOutboundUrlValidator urlValidator,
    ILogger<SafeHttpClient> logger) : ISafeHttpClient
{
    public const int DefaultTimeoutSeconds = 10;
    public const int MaxResponseBytes = 5 * 1024 * 1024;

    public async Task<SafeHttpResponse?> GetStringAsync(string url, CancellationToken ct = default) =>
        (await FetchAsync(url, maxResponseBytes: null, ct)).Response;

    public Task<SafeHttpFetchResult> FetchAsync(string url, CancellationToken ct = default) =>
        FetchAsync(url, maxResponseBytes: null, ct);

    public async Task<SafeHttpFetchResult> FetchAsync(string url, int? maxResponseBytes, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return SafeHttpFetchResult.Fail(SafeHttpFailureKind.EmptyUrl, "empty_url");
        }

        var validation = await urlValidator.ValidateAsync(url, ct);
        if (!validation.IsAllowed)
        {
            logger.LogWarning("SSRF blocked fetch for {Url}: {Reason}", url, validation.Reason);
            return SafeHttpFetchResult.Fail(SafeHttpFailureKind.Ssrf, validation.Reason ?? "ssrf");
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
                    return SafeHttpFetchResult.Fail(
                        SafeHttpFailureKind.Ssrf,
                        redirectValidation.Reason ?? "ssrf_redirect");
                }

                currentUrl = nextUri.ToString();
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return new SafeHttpFetchResult(
                    new SafeHttpResponse(
                        string.Empty,
                        response.Content.Headers.ContentType?.MediaType ?? "text/plain",
                        response.StatusCode,
                        currentUrl),
                    SafeHttpFailureKind.HttpStatus,
                    $"http_{(int)response.StatusCode}");
            }

            var limit = maxResponseBytes is > 0 ? maxResponseBytes.Value : MaxResponseBytes;
            var contentLength = response.Content.Headers.ContentLength;
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new MemoryStream();
            var buffer = new byte[8192];
            var total = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                if (total + read > limit)
                {
                    var allowed = Math.Max(0, limit - total);
                    if (allowed > 0)
                    {
                        await reader.WriteAsync(buffer.AsMemory(0, allowed), ct);
                    }

                    total += read;
                    reader.Position = 0;
                    using var partialReader = new StreamReader(reader, leaveOpen: true);
                    var partial = await partialReader.ReadToEndAsync(ct);
                    var partialContentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";
                    logger.LogWarning(
                        "Response exceeded max size for {Url}: read={Total} limit={Limit} contentLength={ContentLength}.",
                        currentUrl,
                        total,
                        limit,
                        contentLength);
                    return new SafeHttpFetchResult(
                        new SafeHttpResponse(partial, partialContentType, response.StatusCode, currentUrl),
                        SafeHttpFailureKind.Oversized,
                        $"oversized bytes_read={total} limit={limit} content_length={contentLength?.ToString() ?? "unknown"}");
                }

                total += read;
                await reader.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";
            if (!IsAllowedContentType(contentType))
            {
                logger.LogWarning("Blocked content type {ContentType} for {Url}.", contentType, currentUrl);
                return SafeHttpFetchResult.Fail(SafeHttpFailureKind.ContentType, contentType);
            }

            reader.Position = 0;
            using var textReader = new StreamReader(reader);
            var body = await textReader.ReadToEndAsync(ct);
            return SafeHttpFetchResult.Ok(new SafeHttpResponse(body, contentType, response.StatusCode, currentUrl));
        }

        logger.LogWarning("Too many redirects for {Url}.", url);
        return SafeHttpFetchResult.Fail(SafeHttpFailureKind.TooManyRedirects, "too_many_redirects");
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
