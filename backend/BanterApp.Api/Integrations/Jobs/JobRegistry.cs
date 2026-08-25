namespace BanterApp.Api.Integrations.Jobs;

public sealed record JobDefinition(
    string Key,
    string HangfireJobId,
    string DisplayName,
    string Description,
    string? DefaultSchedule,
    bool CanRunManually,
    bool CanPause,
    bool IsStub = false);

public static class JobRegistry
{
    public static IReadOnlyList<JobDefinition> All { get; } =
    [
        new("rss.sync", "rss-opinion-sync", "RSS Opinion Sync", "Ingest pundit opinions from RSS feeds.", "*/20 * * * *", true, true),
        new("youtube.search.sync", "youtube-opinion-sync", "YouTube Search Sync", "Search YouTube for pundit prediction videos.", "0 */3 * * *", true, true),
        new("youtube.metadata.sync", "media-ingest", "YouTube Metadata Sync", "Ingest YouTube channel metadata and media items.", "0 */6 * * *", true, true),
        new("youtube.transcript.sync", "pundit-content-enrich", "Content Enrichment", "Fetch transcripts and article bodies for media items.", "*/10 * * * *", true, true),
        new("openai.opinion.extract", "pundit-extraction", "OpenAI Opinion Extraction", "Extract structured pundit opinions via OpenAI.", "*/10 * * * *", true, true),
        new("openai.banter.generate", "feed-banter-enrich", "Feed Banter Generation", "Generate banter-style feed content via OpenAI.", "*/15 * * * *", true, true),
        new("predictions.aggregate.refresh", "prediction-aggregate-refresh", "Prediction Aggregates", "Refresh consensus prediction aggregates.", null, true, false),
        new("failed-items.retry", "failed-items-retry", "Failed Items Retry", "Requeue failed media items for reprocessing.", null, true, false, IsStub: true),
        new("stale-content.cleanup", "stale-content-cleanup", "Stale Content Cleanup", "Archive or clean up old skipped content.", null, true, true, IsStub: true),
        new("analytics.retention.cleanup", "analytics-retention-cleanup", "Analytics Retention Cleanup", "Delete raw analytics events past the configured retention window.", "30 3 * * *", true, true),
        new("score-sync", "score-sync", "Live Scores Sync", "Sync live match scores.", "*/15 * * * *", true, true),
        new("match-details-sync", "match-details-sync", "Match Details Sync", "Sync match events and lineups.", "*/15 * * * *", true, true),
        new("standings-sync", "standings-sync", "Standings Sync", "Sync tournament standings.", "0 */6 * * *", true, true),
        new("news-ingest", "news-ingest", "News Ingest", "Ingest news articles for the feed.", "0 */2 * * *", true, true),
        new("ai-reactions", "ai-reactions", "AI Reactions", "Generate AI reactions on news items.", "*/20 * * * *", true, true),
        new("football.countries.sync", "football-countries-sync", "Football Countries Sync", "Sync national teams/countries from football API.", "0 4 * * *", true, true),
        new("football.players.sync", "football-players-sync", "Football Players Sync", "Sync tournament squads/players.", "0 5 * * *", true, true),
        new("football.player_stats.sync", "football-player-stats-sync", "Football Player Stats Sync", "Sync player statistics.", "0 6 * * *", true, true),
        new("football.top_scorers.sync", "football-top-scorers-sync", "Top Scorers Sync", "Sync top goal scorers leaderboard.", "*/30 * * * *", true, true),
        new("football.top_assists.sync", "football-top-assists-sync", "Top Assists Sync", "Sync top assists leaderboard.", "*/30 * * * *", true, true),
        new("football.reference_data.full_sync", "football-reference-full-sync", "Full Football Reference Sync", "Sync all football reference data.", null, true, false),
    ];

    public static JobDefinition? FindByKey(string jobKey) =>
        All.FirstOrDefault(j => string.Equals(j.Key, jobKey, StringComparison.OrdinalIgnoreCase));

    public static JobDefinition? FindByHangfireId(string hangfireJobId) =>
        All.FirstOrDefault(j => string.Equals(j.HangfireJobId, hangfireJobId, StringComparison.OrdinalIgnoreCase));
}
