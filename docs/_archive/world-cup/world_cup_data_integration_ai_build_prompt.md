# World Cup Data Integration Enhancement Prompt for AI Developer

## Context

We already have most of the World Cup prediction website built. The task is **not to rebuild everything from scratch**. The task is to inspect the existing codebase, compare it against the target integration design below, identify gaps, and implement only the missing or weak parts.

The product allows family and friends to join prediction leagues for the World Cup. Users make predictions, compare their scores against friends and against public pundits/journalists/podcasters/YouTubers, and generate pre-match/post-match banter scripts based on predictions, results, pundit opinions, and match events.

## Main Objective

Build or enhance the data ingestion layer so the site can reliably ingest:

1. Teams, squads, players, and team/player stats
2. Fixtures
3. Results
4. Standings
5. Match events
6. Lineups
7. Bracket/knockout state
8. Pundit/journalist/podcast/YouTube prediction data
9. Evidence/source links for extracted public predictions

## Important Instruction

Before implementing anything:

1. Inspect the current codebase.
2. Identify what already exists.
3. Compare the existing implementation against this target design.
4. Do not duplicate working functionality.
5. Add missing provider adapters, sync jobs, tables, fields, error handling, or configuration only where needed.
6. If the existing names differ, adapt this plan to the existing naming conventions.
7. Preserve existing production data and migrations unless a migration is clearly required.
8. Add tests or smoke checks for each integration.

## Provider Priority

Use this provider order:

```text
Canonical match data: API-Football / API-Sports
Validation and fallback: Sportmonks Football API
Simple fallback: football-data.org
Official reference: FIFA public pages, where permitted
Pundit/video discovery: YouTube Data API
Podcast discovery: RSS feeds
Sports website discovery: RSS feeds and permitted crawling only
```

## Environment Variables

Add these if not already present:

```env
PRIMARY_FOOTBALL_PROVIDER=api_football

API_FOOTBALL_BASE_URL=https://v3.football.api-sports.io
API_FOOTBALL_KEY=

SPORTMONKS_BASE_URL=https://api.sportmonks.com/v3/football
SPORTMONKS_TOKEN=

FOOTBALL_DATA_BASE_URL=https://api.football-data.org/v4
FOOTBALL_DATA_TOKEN=

YOUTUBE_BASE_URL=https://www.googleapis.com/youtube/v3
YOUTUBE_API_KEY=

OPENAI_API_KEY=
# Or whichever transcription/LLM provider is already used in the project
```

Never hardcode secrets. Use existing project secret-management conventions.

## 1. API-Football / API-Sports Integration

Use API-Football as the primary canonical provider for World Cup football data.

Documentation:

- https://www.api-football.com/documentation-v3
- https://api-sports.io/documentation/football/v3

Authentication:

```http
x-apisports-key: ${API_FOOTBALL_KEY}
```

Base URL:

```text
https://v3.football.api-sports.io
```

Key endpoints to support:

```text
GET /leagues?search=world cup
GET /fixtures?league={world_cup_league_id}&season=2026
GET /standings?league={world_cup_league_id}&season=2026
GET /teams?league={world_cup_league_id}&season=2026
GET /players/squads?team={team_id}
GET /fixtures/events?fixture={fixture_id}
GET /fixtures/lineups?fixture={fixture_id}
GET /fixtures/statistics?fixture={fixture_id}
```

Implementation tasks:

```text
- Check whether an API-Football adapter already exists.
- If it exists, verify it covers all required endpoints above.
- If it does not exist, create an adapter/service class.
- Normalize external provider fields into internal tables/models.
- Store provider IDs in an external_ids table or equivalent mapping.
- Add retry logic and rate-limit handling.
- Add sync logs and error capture.
```

Suggested sync jobs:

```text
syncLeagues()
syncWorldCupFixtures()
syncTeams()
syncSquads()
syncStandings()
syncFixtureResults()
syncMatchEvents()
syncLineups()
syncStats()
```

Suggested polling:

```text
Before tournament: fixtures, teams, squads once daily
Match day but not live: fixtures/results every 5 minutes
Live match window: events, fixture status, score, lineups every 30–60 seconds if plan limits allow
After match: final result and stats every 5 minutes for 1 hour
Off tournament: daily or manual sync only
```

## 2. Sportmonks Integration

Use Sportmonks as the secondary provider for fallback, validation, and potentially richer squads/stats.

Documentation:

- https://docs.sportmonks.com/
- https://www.sportmonks.com/football-api/world-cup-api/

Base URL:

```text
https://api.sportmonks.com/v3/football
```

Authentication:

Sportmonks usually accepts the API token as a request parameter:

```text
?api_token=${SPORTMONKS_TOKEN}
```

Some clients may support token configuration internally. Follow the current project pattern.

Typical calls to support:

```text
GET /leagues?api_token={token}&filters=search:World Cup
GET /fixtures?api_token={token}&include=participants;scores;league;season;stage;round
GET /standings/seasons/{season_id}?api_token={token}
GET /squads/seasons/{season_id}/teams/{team_id}?api_token={token}
```

Implementation tasks:

```text
- Check whether a Sportmonks adapter already exists.
- If it exists, verify it can fetch fixtures, scores, standings, squads, teams, and relevant stats.
- Implement external ID mapping between Sportmonks IDs and canonical fixtures/teams.
- Add discrepancy logging when Sportmonks disagrees with API-Football.
- Do not overwrite canonical data automatically unless the project already has a reconciliation strategy.
```

Fallback strategy:

```text
- API-Football remains canonical.
- Sportmonks fills missing fields when API-Football has null/empty data.
- Sportmonks can flag data inconsistencies for manual review.
- Store source confidence and last_synced_at per provider record where useful.
```

## 3. football-data.org Integration

Use football-data.org as a simpler fallback source.

Documentation:

- https://www.football-data.org/documentation/api

Base URL:

```text
https://api.football-data.org/v4
```

Authentication:

```http
X-Auth-Token: ${FOOTBALL_DATA_TOKEN}
```

Endpoints to support if useful:

```text
GET /competitions
GET /competitions/{competition_code}/matches?season=2026
GET /competitions/{competition_code}/teams?season=2026
GET /competitions/{competition_code}/standings
```

Implementation tasks:

```text
- Check if football-data.org is already integrated.
- Add it only as fallback; do not make it canonical unless existing architecture already uses it.
- Store provider IDs.
- Use it for simple fixture/team/standing verification.
```

## 4. Official FIFA Public Pages

Use FIFA public pages only as a public reference/check, not as the main data pipeline unless explicitly allowed by terms and robots.txt.

Reference:

- https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/scores-fixtures

Implementation rules:

```text
- Prefer official APIs or licensed providers over scraping.
- If scraping is added, check robots.txt and terms first.
- Store minimal factual data only.
- Cache politely.
- Never hammer FIFA pages for live updates.
```

## 5. YouTube Data API Integration

Use YouTube Data API for discovering videos and channels from pundits, podcasts, sports broadcasters, and football analysis creators.

Documentation:

- https://developers.google.com/youtube/v3/docs
- https://developers.google.com/youtube/v3/getting-started
- https://developers.google.com/youtube/v3/docs/captions

Authentication:

For public metadata, use an API key:

```text
key=${YOUTUBE_API_KEY}
```

For captions or private/user-authorized operations, OAuth may be required.

Endpoints to support:

```text
GET /search?part=snippet&channelId={channel_id}&q=World Cup prediction&type=video&key={key}
GET /videos?part=snippet,contentDetails,statistics&id={video_id}&key={key}
GET /channels?part=snippet,contentDetails&id={channel_id}&key={key}
GET /playlistItems?part=snippet&playlistId={uploads_playlist_id}&key={key}
```

Caption/transcript handling:

```text
- The YouTube Data API can list caption tracks, but downloading captions is not always available for public use and may require OAuth/permissions.
- Do not assume every video has accessible captions.
- If captions are not available, only transcribe audio if the project has rights/permission and the source terms allow it.
- Store structured predictions and short evidence snippets, not full transcripts for republication.
```

Implementation tasks:

```text
- Add media source records for YouTube channels.
- Fetch recent videos matching World Cup prediction terms.
- Store video metadata.
- Attempt permitted transcript/caption extraction.
- Run prediction extraction against transcript text.
- Store normalized pundit predictions.
```

Suggested YouTube search terms:

```text
World Cup predictions
World Cup preview
World Cup score prediction
{team_a} vs {team_b} prediction
World Cup group predictions
World Cup winner prediction
```

## 6. Podcast RSS Integration

Use podcast RSS feeds to discover episodes from football shows and pundit podcasts.

Implementation tasks:

```text
- Create a podcast RSS adapter.
- For each configured RSS URL, fetch feed metadata and episodes.
- Store episode title, description, URL, audio URL, publication date, and source.
- Download/transcribe only if terms allow it.
- Extract predictions into structured records.
```

Podcast source configuration shape:

```json
{
  "name": "The Rest Is Football",
  "type": "podcast",
  "rss_url": "",
  "site_url": "",
  "crawl_allowed": true,
  "extract_predictions": true
}
```

## 7. Sports Website RSS / Permitted Crawling

Use RSS feeds, public pages, and permitted crawling to identify articles that contain public predictions.

Potential sources to configure:

```text
BBC Sport
Sky Sports Football
The Guardian Football
ESPN FC
The Athletic
Reuters Sports
FourFourTwo
Goal
CBS Sports Golazo
Fox Soccer
FIFA.com
```

Rules:

```text
- Prefer RSS feeds and sitemaps over scraping HTML pages.
- Check robots.txt before crawling.
- Respect source terms.
- Store extracted prediction facts, source URL, author, publication date, and short evidence snippets only.
- Do not republish full articles.
- Do not store full article text unless there is a clear legal/product reason and terms allow it.
```

Website source configuration shape:

```json
{
  "name": "BBC Sport",
  "type": "website",
  "rss_url": "",
  "base_url": "",
  "robots_url": "",
  "crawl_allowed": null,
  "extract_predictions": true
}
```

## 8. Pundit Prediction Extraction

Use an LLM or extraction service to convert transcripts/articles into normalized predictions.

Prediction output schema:

```json
{
  "source_name": "The Rest Is Football",
  "source_type": "podcast | youtube | website | journalist | pundit",
  "speaker": "Unknown or detected speaker",
  "author": "Article author if available",
  "match": "England vs Brazil",
  "fixture_id": "internal fixture id if matched",
  "prediction_type": "winner | scoreline | draw | group_winner | tournament_winner | top_scorer | qualifier | upset",
  "predicted_team": "England",
  "predicted_score": "2-1",
  "confidence": 0.78,
  "evidence_snippet": "short quote only",
  "source_url": "episode/video/article URL",
  "published_at": "2026-06-10T10:00:00Z"
}
```

Extraction rules:

```text
- Match predictions to internal fixture IDs where possible.
- If fixture matching is uncertain, store unmatched prediction for review.
- Use confidence scoring.
- Keep evidence snippets short.
- Store source URL and timestamp.
- Avoid publishing unsupported guesses.
```

## 9. Suggested Database Tables / Models

Compare these against the existing schema. Add only missing fields/tables.

```text
data_sources
external_ids
teams
players
fixtures
fixture_results
match_events
lineups
team_stats
player_stats
standings
bracket_nodes
sync_runs
sync_errors

media_sources
media_items
transcripts
pundits
pundit_predictions
prediction_evidence
```

Important fields for `external_ids`:

```text
entity_type: team | player | fixture | competition | season | source | media_item
entity_id: internal ID
provider: api_football | sportmonks | football_data | fifa | youtube | rss | website
external_id: provider's ID
last_seen_at
raw_payload_hash
```

Important fields for `sync_runs`:

```text
provider
job_name
started_at
finished_at
status
records_created
records_updated
records_failed
error_message
```

## 10. Bracket Handling

The knockout bracket should be derived from canonical fixtures and standings, not hardcoded.

Implementation tasks:

```text
- Confirm whether bracket_nodes or equivalent already exists.
- Link each knockout fixture to bracket round/stage.
- Store winner progression once result is final.
- Support placeholders before teams are known, e.g. Winner Group A vs Runner-up Group B.
- Update bracket from canonical fixture/result sync.
```

## 11. Banter and Script Generation Inputs

Make sure script generation can use:

```text
- User prediction
- Friend league standings
- Pundit consensus
- Actual result
- Scoreline
- Major match events
- Lineups where relevant
- Team/player stats
- Prediction accuracy history
```

Generated script types:

```text
pre_match_panel_script
post_match_roast
fake_pundit_debate
youtube_shorts_script
tiktok_voiceover
whatsapp_banter_message
```

Safety/product rule:

```text
Do not clone or impersonate a real pundit's voice, likeness, or exact persona.
Use generic styles such as "dramatic football pundit panel", "pub banter", "tactical analyst", or "overexcited matchday host".
```

## 12. Acceptance Criteria

The work is complete when:

```text
- Existing implementation has been inspected and compared against this target design.
- Missing environment variables are documented.
- API-Football ingestion works for fixtures, teams, standings, results, squads, events, lineups, and stats where available.
- Sportmonks fallback/validation works or is clearly stubbed with TODOs.
- football-data.org fallback works or is clearly stubbed with TODOs.
- YouTube metadata ingestion works for configured channels.
- Podcast RSS ingestion works for configured feeds.
- Website RSS/permitted crawling framework exists with compliance guardrails.
- Pundit prediction extraction stores structured predictions with source URLs and short evidence snippets.
- Sync jobs are idempotent.
- Provider IDs are stored and mapped.
- Sync logs and errors are visible to admins/developers.
- Secrets are not committed.
- Tests or smoke scripts exist for each provider adapter.
```

## 13. First Implementation Order

```text
1. Inspect current code and schema.
2. Produce a gap report.
3. Implement missing env/config.
4. Complete API-Football adapter first.
5. Add external ID mapping if missing.
6. Add sync logging if missing.
7. Add Sportmonks fallback.
8. Add football-data.org fallback.
9. Add YouTube media discovery.
10. Add podcast RSS ingestion.
11. Add website RSS/permitted crawler framework.
12. Add prediction extraction and fixture matching.
13. Add admin/debug view or logs for sync status.
```

## 14. Deliverables From AI Developer

Ask the AI developer to return:

```text
- Gap report: what already existed vs what was added.
- List of files changed.
- Environment variables required.
- Database migrations added.
- How to run each sync job locally.
- How to test each provider.
- Known limitations and TODOs.
```
