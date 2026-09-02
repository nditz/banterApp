using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace BanterApp.Api.Integrations.Rss;

public sealed class FileRssFeedCatalogSeed : IRssFeedCatalogSeed
{
    public const string RelativePath = "config/rss-feed-catalog.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public FileRssFeedCatalogSeed(IHostEnvironment env, ILogger<FileRssFeedCatalogSeed> logger)
    {
        var path = Path.Combine(env.ContentRootPath, "config", "rss-feed-catalog.json");
        if (!File.Exists(path))
        {
            logger.LogWarning("RSS catalog seed file missing at {Path}.", path);
            Feeds = [];
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var document = JsonSerializer.Deserialize<RssFeedCatalogFile>(json, JsonOptions);
            Feeds = (document?.Feeds ?? [])
                .Where(f => !string.IsNullOrWhiteSpace(f.RssUrl) && !string.IsNullOrWhiteSpace(f.Name))
                .ToList();
            logger.LogInformation("Loaded {Count} RSS catalog seed entries from {Path}.", Feeds.Count, path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load RSS catalog seed from {Path}.", path);
            Feeds = [];
        }
    }

    public IReadOnlyList<RssFeedSeedEntry> Feeds { get; }

    private sealed class RssFeedCatalogFile
    {
        public List<RssFeedSeedEntry> Feeds { get; set; } = [];
    }
}
