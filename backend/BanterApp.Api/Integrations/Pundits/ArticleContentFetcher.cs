using System.Text.RegularExpressions;
using BanterApp.Api.Services;

namespace BanterApp.Api.Integrations.Pundits;

public interface IArticleContentFetcher
{
    Task<string?> FetchArticleTextAsync(string url, CancellationToken cancellationToken = default);
}

public sealed partial class ArticleContentFetcher : IArticleContentFetcher
{
    private readonly ISafeHttpClient _safeHttpClient;
    private readonly ILogger<ArticleContentFetcher> _logger;

    public ArticleContentFetcher(ISafeHttpClient safeHttpClient, ILogger<ArticleContentFetcher> logger)
    {
        _safeHttpClient = safeHttpClient;
        _logger = logger;
    }

    public async Task<string?> FetchArticleTextAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            var response = await _safeHttpClient.GetStringAsync(url, cancellationToken);
            if (response is null || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("Article fetch failed or blocked for {Url}.", url);
                return null;
            }

            return StripHtml(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Article fetch failed for {Url}.", url);
            return null;
        }
    }

    internal static string? StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var withoutScripts = ScriptTagRegex().Replace(html, string.Empty);
        var text = TagRegex().Replace(withoutScripts, " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = WhitespaceRegex().Replace(text, " ").Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    [GeneratedRegex("<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
