# BanterApp Integrations (Phase 1)

Phase 1 integration abstractions for sports data, news feeds, and stubbed AI content.

## Wiring into DI

Integrations are registered in `Program.cs` via `AddBanterIntegrations()`.

You can also call from any `IServiceCollection` setup (e.g. feature modules or test fixtures):

```csharp
services.AddBanterIntegrations();
```

## Environment variables

| Variable | Values | Default | Effect |
|----------|--------|---------|--------|
| `SPORTS_API_PROVIDER` | `mock`, `apifootball` | `mock` | Selects sports data provider |
| `SPORTS_API_KEY` | API-Football key | _(empty)_ | Required for live API-Football calls; missing key falls back to mock |
| `NEWS_API_KEY` | NewsAPI.org key | _(empty)_ | When set, uses NewsAPI; otherwise mock news |

Copy from repo root `.env.example` and set values in your environment or user secrets.

## Provider selection logic

### Sports data (`ISportsDataProvider`)

1. Read `SPORTS_API_PROVIDER` (default: `mock`).
2. **`mock`** — registers `MockSportsDataProvider` (12 World Cup 2026–style fixtures, 4 finished + 8 upcoming).
3. **`apifootball`** — registers `ApiFootballProvider` via typed `HttpClient`. Reads `SPORTS_API_KEY` from env. If the key is missing or API calls fail, each method gracefully falls back to mock data.
4. Unknown values log a warning and fall back to mock.

`SportsDataSyncService` runs as a hosted background service and periodically logs fixture counts (DB upsert deferred to Phase 2).

### News feed (`INewsProvider`)

1. If `NEWS_API_KEY` is set → `NewsApiProvider` (NewsAPI.org skeleton with basic JSON mapping).
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
