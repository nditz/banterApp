namespace BanterApp.Api.Data.Entities;

public class NewsFeedItem
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Category { get; set; }
    /// <summary>Links AI-generated reaction posts back to the source article or match item.</summary>
    public string? ParentItemId { get; set; }
    public string? ImageUrl { get; set; }
    /// <summary>image or gif — pairs with <see cref="ImageUrl"/>.</summary>
    public string? MediaType { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public int ViewCount { get; set; }
}
