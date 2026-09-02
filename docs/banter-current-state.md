# Banter current-state audit (Phase A)

**Date:** 2026-09-02  
**Baseline:** `BanterApp.Api` build succeeded (1 pre-existing warning in `AdminOverviewService`). Targeted banter/GIF tests: **25 passed** (`GiphyGif*`, `ReactionMedia*`, `FeedReaction*`, `FootballBanter*`).

## How banter is generated today

Banter is **not** a single post-prediction pipeline. Three stacks exist:

1. **Hangfire feed enrichment** (`FeedBanterEnrichmentJob`) — highest leverage for live GIFs  
   - `IFootballBanterEngine` rewrites feed copy  
   - `ReactionMediaResolver` → `IReactionGifProvider` (Giphy) + `IReactionGifLedger`  
2. **Hangfire AI reactions** (`AiReactionJob`) — child `ai_reaction` cards via `IContentGenerator` + same media resolver  
3. **On-demand API** (`POST /api/ai/banter`, `/meme`, …) via `IContentGenerator` — frontend does not call these today  
4. **Client-side prediction reactions** — local rules + SVG stickers (`frontend/src/lib/reactionEngine.ts`); not server LLM/Giphy

```text
NewsFeedItem / headline
        |
        v
IFootballBanterEngine / IContentGenerator  (text)
        |
        v
ReactionMediaResolver.ResolveAsync(queries, mood, seed)
        |
        +--> IReactionGifProvider.FindGifUrlAsync  (Giphy random/search + claim)
        +--> IReactionGifLedger (gameweek uniqueness)
        +--> FeedGifCatalog stickers (fallback)
        |
        v
NewsFeedItem.ImageUrl / FeedMediaResponse { type, url }
```

## Safe insertion point (Strategy Engine seam)

Introduce **`IBanterGenerator`** behind `Banter:UseStrategyEngine` (default **false**):

| Path | Behavior |
|------|----------|
| Flag OFF | `LegacyBanterGenerator` → existing `ReactionMediaResolver` |
| Flag ON | `BanterOrchestrator` → classify → concepts → multi-pool Giphy → history exclude → score → weighted select → map to same media shape |

**Wire call sites first (GIF path only; keep text engines):**

- `FeedBanterEnrichmentJob` (after banter rewrite, instead of raw `ReactionMediaResolver`)
- `AiReactionJob` (same)

Do **not** invent a parallel OpenAI/Giphy stack. Reuse `IContentGenerator` / `AiOptions` for concept JSON, `ReactionGifOptions` + Giphy search for pools, and keep `ReactionMediaResolver` as the legacy + progressive fallback.

Placement: `Integrations/Banter/` (matches existing `Integrations/*` convention; no Application/Infrastructure layers in this repo).

## Contracts to preserve

- `AiGenerationResponse` (`Content`, `Type`, `RemainingGenerations`, `ImageUrl`)
- `FeedItemResponse` / `FeedMediaResponse` (`Type`: `gif` | `image`, `Url`, `Alt`)
- Internal: `FeedBanterCard`, `FeedVisualSuggestion`, `FootballBanterOutput`, `ReactionMedia`

Strategy metadata (scenario, scores, concepts) stays **internal** (history table + logs).

## Existing exclusion / history

| Mechanism | Scope | Gap vs kit |
|-----------|--------|------------|
| `reaction_gif_uses` + `IReactionGifLedger` | Gameweek window GIF identity | No user/team/scenario/search-phrase history |
| `generated_content` | AI API audit | Not used for dedupe |
| Feed “already banterized” markers | Per item | N/A |

**Net-new:** additive `banter_content_history` + `IBanterHistoryService` with configurable user/team/global windows.

## DB / migration conventions

- EF Core, fluent config in `AppDbContext`, tables snake_case  
- Migrations: `Data/Migrations/{yyyyMMddHHmmss}_{Name}.cs`  
- Latest before this work: `20260901125602_ReactionGifUses`  
- Ids: mix of `Guid` (predictions/users) and `string` (feed/match ids)

## Config conventions

`services.Configure<T>(configuration.GetSection(T.SectionName))` in `AddBanterIntegrations`.  
Kit `"Banter"` section does not exist yet — add alongside `Ai` / `ReactionGif`.

## Tests

- Project: `BanterApp.Api.Tests` (xUnit)  
- Patterns: `{Subject}Tests`, in-memory EF where needed (`GiphyGifProviderTests`)

## Deliberately out of scope for Strategy Engine workstream

- Premier League timeline / `MatchEndpoints` / `PremierLeagueMatchScope` / `ScoreSyncJob` / World Cup purge (parallel agent)  
- Frontend redesign; Phase 4 meme templates; Phase 5 engagement analytics  
- Vector search / microservices

## Adapted class mapping (kit → repo)

| Kit name | Repo adaptation |
|----------|-----------------|
| `IBanterGenerator` | New; feature-flagged facade |
| `LegacyBanterGenerator` | Wraps `ReactionMediaResolver` |
| `BanterOrchestrator` | New strategy path |
| Candidate provider | Giphy pool search (reuse `GiphyResponseParser` / options) |
| History | New entity + service (complement ledger, do not replace) |
