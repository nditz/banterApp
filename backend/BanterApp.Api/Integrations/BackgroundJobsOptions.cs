namespace BanterApp.Api.Integrations;

/// <summary>
/// Staggered Hangfire job schedule. Each pipeline runs independently on its own interval.
/// Intervals are chosen so jobs rarely collide: live scores (fastest) → AI reactions → news ingest (slowest).
/// </summary>
public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool Enabled { get; set; } = true;

    /// <summary>Live fixtures &amp; scores via API-Football / public score APIs.</summary>
    public int LiveScoresIntervalMinutes { get; set; } = 5;

    /// <summary>Match events and lineups for live/recent fixtures.</summary>
    public int MatchDetailsIntervalMinutes { get; set; } = 5;

    /// <summary>Tournament standings sync from canonical provider.</summary>
    public int StandingsIntervalMinutes { get; set; } = 360;

    /// <summary>Minute offset within the hour for standings sync.</summary>
    public int StandingsStartMinute { get; set; } = 15;

    /// <summary>AI banter reactions to freshly ingested news &amp; match data.</summary>
    public int AiReactionsIntervalMinutes { get; set; } = 20;

    /// <summary>
    /// Sports news scraping, podcast/YouTube transcript ingestion (longest interval).
    /// </summary>
    public int NewsIngestIntervalMinutes { get; set; } = 120;

    /// <summary>YouTube, podcast RSS, and website RSS media discovery.</summary>
    public int MediaIngestIntervalMinutes { get; set; } = 360;

    /// <summary>Minute offset within the hour for AI job (stagger from live scores).</summary>
    public int AiReactionsStartMinute { get; set; } = 7;

    /// <summary>Minute offset within the hour for news ingest (stagger from AI job).</summary>
    public int NewsIngestStartMinute { get; set; } = 30;

    /// <summary>Minute offset within the hour for media ingest.</summary>
    public int MediaIngestStartMinute { get; set; } = 45;

    public int AiReactionsBatchSize { get; set; } = 5;

    /// <summary>Gen Z banter rewrite for RSS, pundit, and match desk feed cards.</summary>
    public int FeedBanterEnrichmentIntervalMinutes { get; set; } = 15;

    public int FeedBanterEnrichmentStartMinute { get; set; } = 12;

    public int FeedBanterEnrichmentBatchSize { get; set; } = 8;

    /// <summary>RSS opinion ingest for pundit extraction pipeline.</summary>
    public int RssOpinionSyncIntervalMinutes { get; set; } = 20;

    public int RssOpinionSyncStartMinute { get; set; } = 5;

    /// <summary>YouTube keyword search for pundit content.</summary>
    public int YouTubeSearchSyncIntervalMinutes { get; set; } = 180;

    public int YouTubeSearchSyncStartMinute { get; set; } = 20;

    /// <summary>OpenAI extraction batch for enriched media items.</summary>
    public int PunditExtractionIntervalMinutes { get; set; } = 10;

    public int PunditExtractionStartMinute { get; set; } = 35;

    /// <summary>Fetch transcripts and article bodies.</summary>
    public int PunditContentEnrichIntervalMinutes { get; set; } = 10;

    public int PunditContentEnrichStartMinute { get; set; } = 10;
}
