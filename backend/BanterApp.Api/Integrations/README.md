# BanterApp Integrations (Phase 1)

Phase 1 integration abstractions for sports data, news feeds, and stubbed AI content.

## Wiring into DI

Integrations are registered in `Program.cs` via `AddBanterIntegrations()`.

You can also call from any `IServiceCollection` setup (e.g. feature modules or test fixtures):

```csharp
services.AddBanterIntegrations(configuration);
```

## Environment variables

Backend settings use **ASP.NET Core configuration** — not repo-root `.env`.

| Local | Deploy |
|-------|--------|
| `appsettings.Development.json` (gitignored) | GitHub Environment secrets / host env vars |

See **`docs/BACKEND-CONFIGURATION.md`** for all keys and GitHub mapping.

| Key | Values | Default | Effect |
|-----|--------|---------|--------|
| `SportsData:Provider` | `mock`, `apifootball` | `mock` | Selects sports data provider |
| `SportsData:ApiKey` | API-Football key | _(empty)_ | Required for live API-Football calls |
| `News:ApiKey` | NewsAPI.org key | _(empty)_ | When set, uses NewsAPI; otherwise mock news |

Copy `appsettings.Development.json.example` → `appsettings.Development.json` and fill in secrets.

## Provider selection logic

### Sports data (`ISportsDataProvider`)

1. Read `SportsData:Provider` from configuration (default: `mock`).
2. **`mock`** — registers `MockSportsDataProvider` (12 World Cup 2026–style fixtures, 4 finished + 8 upcoming).
3. **`apifootball`** — registers `ApiFootballProvider` via typed `HttpClient`. Reads `SportsData:ApiKey`. If the key is missing or API calls fail, each method gracefully falls back to mock data.
4. Unknown values log a warning and fall back to mock.

Three **independent, staggered** Hangfire jobs run in the background (see `BackgroundJobs` in `appsettings.json`):

| Job | Default interval | Purpose |
|-----|------------------|---------|
| `ScoreSyncJob` | 5 min | Live scores + fixtures via API-Football `fixtures?live=all` |
| `AiReactionJob` | 20 min (offset :07) | AI pundit reactions to ingested news/match items |
| `NewsIngestJob` | 120 min (offset :30) | Sports news, fixtures, results, live desk items |

Configure intervals and offsets in `BackgroundJobs`. See `docs/AI-INTEGRATION-PLAN.md` for the full AI rollout plan.

In development the Hangfire dashboard is available at `http://localhost:5000/hangfire`.

### Free score API options (researched from github.com/public-apis)

| API | Free tier | Notes |
|-----|-----------|-------|
| **API-Football** (integrated) | 100 req/day | All endpoints incl. live scores; World Cup `league=1` |
| football-data.org | 10 req/min, 12 competitions | Free forever; includes World Cup; good fallback |
| TheSportsDB | 30 req/min | Crowd-sourced; best for logos/metadata, not live accuracy |

At a 5-minute poll interval (288 req/day) the API-Football free tier is exceeded — either raise `SyncIntervalMinutes` to 15+ (96 req/day) or upgrade to the Pro plan ($19/mo) for live polling.

### News feed (`INewsProvider`)

1. If `News:ApiKey` is set → `NewsApiProvider` (NewsAPI.org with basic JSON mapping).
2. If unset → `MockNewsProvider` (5 sample articles with full attribution).

### AI content (`IContentGenerator`)

Always registers `StubContentGenerator` in Phase 1 — no live LLM calls.

- `CanGenerateAsync(userId, isAnonymous)` — anonymous users limited to **3** generations (in-memory counter).
- Template-based outputs for banter, analysis, meme captions, and video scripts.

## Project layout

```
Integrations/
├── ServiceCollectionExtensions.cs
├── SportsData/
│   ├── ISportsDataProvider.cs
│   ├── MockSportsDataProvider.cs
│   ├── ApiFootballProvider.cs
│   ├── SportsDataOptions.cs
│   ├── SportsDataSyncService.cs
│   └── Dtos/
├── News/
│   ├── INewsProvider.cs
│   ├── MockNewsProvider.cs
│   ├── NewsApiProvider.cs
│   └── NewsArticleDto.cs
└── Ai/
    ├── IContentGenerator.cs
    └── StubContentGenerator.cs
```

## Phase 2 notes

- Map API-Football JSON responses in `ApiFootballProvider.FetchFixturesAsync`.
- Wire `SportsDataSyncService` to EF Core / Supabase upsert.
- Swap `StubContentGenerator` for a live provider behind `IContentGenerator`.
- See `docs/PHASE2-AI-COSTS.md` for token/cost estimates.
