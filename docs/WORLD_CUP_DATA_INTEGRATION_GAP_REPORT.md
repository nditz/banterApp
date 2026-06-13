# World Cup Data Integration — Gap Report & Deliverables

Generated from implementation against `docs/world_cup_data_integration_ai_build_prompt.md`.

## What Already Existed

| Area | Status |
|------|--------|
| API-Football fixtures, live scores, standings API, match statistics | `ApiFootballProvider`, `ApiFootballFixtureMapper` |
| Mock sports data fallback | `MockSportsDataProvider` |
| Score sync Hangfire job | `ScoreSyncJob` |
| NewsAPI sports articles | `NewsApiProvider`, `NewsIngestJob` |
| Pundits + seeded predictions | `Pundit`, `PunditPrediction`, `DatabaseSeeder` |
| Bracket engine (code-defined, not DB) | `BracketEngine`, `BracketTemplate` |
| AI stub content generation | `StubContentGenerator` |

## What Was Added

### Sync infrastructure
- `external_ids`, `sync_runs`, `sync_errors` tables
- `SyncRunTracker` for idempotent run logging and provider ID mapping

### Extended API-Football
- `ApiFootballHttpClient` with retry + rate-limit handling
- `ISportsDataEnrichment`: teams, squads, events, lineups, full standings
- Mappers for events, lineups, teams, squads

### Fallback providers
- `SportmonksProvider` (fixtures; standings TODO when season_id configured)
- `FootballDataProvider` (fixtures + standings)
- Fallback used when canonical data is empty; discrepancies logged to `sync_errors`

### New sync jobs
| Job ID | Purpose |
|--------|---------|
| `score-sync` | Fixtures/scores + external ID mapping (enhanced) |
| `standings-sync` | Persist tournament tables |
| `match-details-sync` | Events + lineups for live/FT matches |
| `media-ingest` | YouTube channels, podcast RSS, website RSS |

### Media ingestion
- `YouTubeProvider` — channel video discovery
- `RssFeedProvider` — RSS/Atom parser (no HTML crawling)
- `MediaSource`, `MediaItem` tables
- `MediaIngestOptions.WebsiteSources` for compliant RSS-only website sources

### Admin / debug API
- `GET /api/sync/runs` — recent sync runs
- `GET /api/sync/errors` — discrepancy and failure log
- `GET /api/sync/status` — counts + provider configuration
- `POST /api/sync/trigger/{jobName}` — manual job trigger

### Tests & smoke checks
- `backend/BanterApp.Api.Tests/` — mapper + mock provider unit tests
- `scripts/test-integrations.ps1` — HTTP smoke script

### Schema
- Migration `20260613083544_WorldCupDataIntegration`
- Extended `pundit_predictions` with source URL, evidence, confidence, etc.

## Environment Variables

Backend (`appsettings.Development.json` or deploy secrets):

| Key | Purpose |
|-----|---------|
| `SportsData:Provider` | `mock` or `apifootball` |
| `SportsData:ApiKey` | API-Football key |
| `Sportmonks:Token` | Sportmonks fallback |
| `FootballData:Token` | football-data.org fallback |
| `YouTube:ApiKey` | YouTube Data API |
| `News:ApiKey` | NewsAPI.org |
| `Ai:ApiKey` | LLM for prediction extraction (future) |

Deploy mapping examples: `Sportmonks__Token`, `FootballData__Token`, `YouTube__ApiKey`.

## How to Run Sync Jobs Locally

1. Stop any running API process, then migrate:
   ```powershell
   .\scripts\run-migrations.ps1
   .\scripts\run-api.ps1
   ```
2. Hangfire dashboard: `http://localhost:5000/hangfire`
3. Manual trigger:
   ```powershell
   Invoke-RestMethod -Method Post http://localhost:5000/api/sync/trigger/score-sync
   Invoke-RestMethod -Method Post http://localhost:5000/api/sync/trigger/standings-sync
   Invoke-RestMethod -Method Post http://localhost:5000/api/sync/trigger/media-ingest
   ```
4. Smoke test: `.\scripts\test-integrations.ps1`
5. Unit tests: `dotnet test backend/BanterApp.Api.Tests`

## Configure Media Sources

In `appsettings.Development.json`:

```json
{
  "MediaIngest": {
    "YouTubeChannelIds": ["UCxxxxxxxx"],
    "PodcastFeedUrls": ["https://feeds.example.com/podcast.rss"],
    "WebsiteSources": [
      {
        "name": "BBC Sport",
        "type": "website",
        "rssUrl": "https://feeds.bbci.co.uk/sport/football/rss.xml",
        "crawlAllowed": true,
        "extractPredictions": true
      }
    ]
  }
}
```

## Known Limitations & TODOs

- **Sportmonks standings** — needs `WorldCupLeagueId`/season_id configuration
- **Pundit prediction extraction** — media items stored; LLM extraction pipeline not wired yet
- **FIFA scraping** — intentionally not implemented (use licensed providers)
- **Teams/players tables** — teams flow through match DTOs + external_ids; dedicated `teams`/`players` tables deferred
- **Bracket DB nodes** — bracket remains code-driven via `BracketEngine`; knockout progression from canonical fixtures is future work
- **Transcripts** — only description snippets stored; no Whisper/YouTube caption download yet
- **Website HTML crawl** — RSS-only framework; robots.txt checker not automated

## Files Changed (summary)

```
backend/BanterApp.Api/Data/Entities/*.cs          (new sync/media entities)
backend/BanterApp.Api/Data/AppDbContext.cs
backend/BanterApp.Api/Data/Migrations/20260613083544_WorldCupDataIntegration.cs
backend/BanterApp.Api/Integrations/Common/SyncRunTracker.cs
backend/BanterApp.Api/Integrations/SportsData/*   (extended + fallback providers + jobs)
backend/BanterApp.Api/Integrations/Media/*        (YouTube, RSS, media ingest)
backend/BanterApp.Api/Features/Sync/SyncEndpoints.cs
backend/BanterApp.Api.Tests/*
scripts/test-integrations.ps1
docs/WORLD_CUP_DATA_INTEGRATION_GAP_REPORT.md
```
