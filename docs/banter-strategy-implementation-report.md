# Banter Strategy Engine — Implementation Report

**Date:** 2026-09-02  
**Scope:** Kit Phases 1–3 behind `Banter:UseStrategyEngine` (default `false`) + Premier League timeline/fixtures hardening  
**Out of scope (deferred):** Phase 4 curated meme templates, Phase 5 engagement analytics  
**Phase D:** Self-audit against `docs/local_docs/banter-cursor-implementation-kit/07-acceptance-checklist.md`

---

## Implemented architecture

```text
FeedBanterEnrichmentJob / AiReactionJob
        |
        v
IBanterGenerator (FeatureFlaggedBanterGenerator)
        |
   +----+------------------+
   | UseStrategyEngine=false | UseStrategyEngine=true
   v                         v
LegacyBanterGenerator     BanterOrchestrator
   |                         |
   |              classify → concepts → Giphy pools
   |              → history exclude → score → weighted select
   |                         |
   +-------- ReactionMediaResolver (legacy + progressive fallback)
                         |
                         v
              BanterContentHistory (additive)
```

| Concern | Implementation |
|---------|----------------|
| Seam | `IBanterGenerator` → `FeatureFlaggedBanterGenerator` |
| Legacy | `LegacyBanterGenerator` → existing `ReactionMediaResolver` |
| Orchestrator | `BanterOrchestrator` |
| Scenario | `DeterministicBanterScenarioClassifier` (`BanterScenario` enum) |
| Concepts | `OpenAiBanterConceptGenerator` + `PredefinedBanterConcepts` |
| Candidates | `GiphyBanterCandidateProvider` |
| History | `BanterHistoryService` / `banter_content_history` |
| Scoring | `BanterCandidateScorer` (relevance / freshness / popularity / novelty) |
| Selection | `WeightedBanterCandidateSelector` + `IBanterRandom` / `SeededBanterRandom` |
| Context | `BanterContextFactory` (feed/job request mapping) |

Public AI endpoints (`POST /api/ai/banter`, `/meme`, …) and prediction scoring are **unchanged**. Strategy Engine is wired into Hangfire GIF enrichment paths only (`FeedBanterEnrichmentJob`, `AiReactionJob`).

---

## Files changed

### Strategy Engine

| Area | Paths |
|------|-------|
| Core | `backend/BanterApp.Api/Integrations/Banter/*` |
| DI | `backend/BanterApp.Api/Integrations/ServiceCollectionExtensions.cs` |
| Jobs | `backend/BanterApp.Api/Integrations/Ai/FeedBanterEnrichmentJob.cs`, `AiReactionJob.cs` |
| Entity | `backend/BanterApp.Api/Data/Entities/BanterContentHistory.cs` |
| DbContext | `backend/BanterApp.Api/Data/AppDbContext.cs` |
| Config | `backend/BanterApp.Api/appsettings.json` (`Banter` section) |
| Audit doc | `docs/banter-current-state.md` |
| Tests | `backend/BanterApp.Api.Tests/Banter*Tests.cs`, `FeatureFlaggedBanterGeneratorTests.cs`, `OpenAiBanterConceptGeneratorTests.cs`, `DeterministicBanterScenarioClassifierTests.cs`, `WeightedBanterCandidateSelectorTests.cs` |

### Premier League workstream (completed earlier; verified in Phase D)

| Area | Paths |
|------|-------|
| Scope | `PremierLeagueMatchScope.cs` |
| Sync | `ScoreSyncJob.cs`, `ApiFootballProvider.cs` / related |
| Endpoints | `MatchEndpoints.cs` (PL filter on DB + provider fallbacks) |
| Purge | `WorldCupLegacyPurge.cs` |
| Feed | `PersonalizedFeedService.cs` (skip non-PL personal cards) |
| Frontend | `frontend/src/lib/mock-data.ts`, `postMatchResults.ts` |
| Tests | `PremierLeagueMatchScopeTests.cs`, `WorldCupLegacyPurgeTests.cs`, match API coverage |

---

## Migrations

| Migration | Purpose |
|-----------|---------|
| `20260902211014_AddBanterContentHistory` | Additive `banter_content_history` table + indexes + RLS enable |

**Columns:** `UserId`, `MatchId`, `TeamId`, `PredictionId`, `ScenarioType`, `ContentType`, `Provider`, `ProviderContentId`, `SearchPhrase`, `MemeTemplateId`, `CaptionHash`, `SelectionScore`, `UsedAtUtc`.

**Safety:** Create-only; legacy path does not require the table. Rollback of the feature flag does **not** require a DB downgrade.

---

## Tests added

| Test class | Coverage |
|------------|----------|
| `BanterHistoryServiceTests` | Empty history; GIF / template / search-phrase exclusion; window expiry; `RecordAsync` |
| `DeterministicBanterScenarioClassifierTests` | Win/draw/upset/aged-badly paths |
| `OpenAiBanterConceptGeneratorTests` | Parse structured JSON; malformed → empty; normalize/dedupe/exclude; predefined merge; **HTTP 500 → predefined fallback** |
| `FeatureFlaggedBanterGeneratorTests` | **Flag OFF → legacy**; **Flag ON → strategy** (gif URL + scenario) |
| `BanterCandidateScorerTests` | Novelty/relevance/weights/exclusion; weight normalize |
| `WeightedBanterCandidateSelectorTests` | Empty/single/top-N; seeded weighted preference |
| `BanterOrchestratorFallbackTests` | Empty Giphy pool → legacy; **provider throw → legacy**; DTO media-type contract |
| PL | `PremierLeagueMatchScopeTests`, `WorldCupLegacyPurgeTests` |

---

## Configuration keys

Section: `Banter` (`BanterOptions`)

| Key | Default | Notes |
|-----|---------|-------|
| `UseStrategyEngine` | `false` | Feature flag |
| `RecentContentWindowDays` | `30` | Per-user exclusion |
| `RecentTeamContentWindowDays` | `14` | Per-team exclusion |
| `GlobalHardRepeatWindowDays` | `3` | Global provider-id hard repeat |
| `ConceptCount` | `12` | Concepts requested / predefined fill |
| `ConceptsUsedPerGeneration` | `4` | Concepts searched per event |
| `CandidatesPerConcept` | `15` | Giphy hits per concept |
| `TopCandidatePoolSize` | `15` | Weighted selection pool |
| `Weights.Relevance` | `0.40` | Normalized at startup |
| `Weights.Freshness` | `0.25` | Neutral `0.5` score in scorer (Giphy timestamps not parsed) |
| `Weights.Popularity` | `0.15` | Derived from provider rank |
| `Weights.Novelty` | `0.20` | Lower when phrase recently used |

Validation: `BanterOptions.ValidateOrNormalize()` via `PostConfigure`.

---

## Feature-flag / fallback behavior

| Mode | Behavior |
|------|----------|
| `UseStrategyEngine=false` (default / prod initial) | `LegacyBanterGenerator` only → `ReactionMediaResolver` |
| `UseStrategyEngine=true` | Full orchestrator pipeline |
| Empty / failed Giphy pools | Progressive relaxation of exclusions, then **legacy resolver** (`BanterFallbackUsed`) |
| OpenAI concept failure / no API key | `PredefinedBanterConcepts` |
| History read/write failure | Logged warning; generation continues |

**Enable (staging/local):** set `Banter:UseStrategyEngine=true`  
**Rollback:** set `Banter:UseStrategyEngine=false` (config only; no migration downgrade)

---

## PL workstream acceptance (brief)

| Criterion | Status | Evidence |
|-----------|--------|----------|
| Match APIs PL-only | PASS | `MatchEndpoints` uses `WherePremierLeague` / `FilterPremierLeagueDtos` |
| Scope hardened | PASS | `PremierLeagueMatchScope` — bare `apifb-*` insufficient; WC-shaped rows rejected |
| Purge | PASS | `WorldCupLegacyPurge` + tests for mis-stamped WC rows |
| Feed personal cards | PASS | `PersonalizedFeedService` skips `!IsPremierLeague(match)` |
| Frontend mocks | PASS | PL clubs in `postMatchResults.ts`; no WC leftovers in `mock-data.ts` |

---

## Completed acceptance checklist

Mark legend: **PASS** / **FAIL** / **NOT APPLICABLE** / **BLOCKED**

### Architecture

| Item | Status | Evidence |
|------|--------|----------|
| Existing public banter endpoint(s) preserved | PASS | `Features/Ai/AiEndpoints.cs` signatures unchanged |
| Existing frontend response contract preserved | PASS | Jobs set `ImageUrl` / `MediaType`; `AiGenerationResponse` / feed DTOs unchanged |
| New strategy behind interface/orchestrator seam | PASS | `IBanterGenerator`, `BanterOrchestrator`, `FeatureFlaggedBanterGenerator` |
| Legacy generator available as fallback | PASS | `LegacyBanterGenerator`; orchestrator `FallbackToLegacyAsync` |
| Feature flag switches legacy vs engine | PASS | `Banter:UseStrategyEngine` |
| No unnecessary rewrite of prediction logic | PASS | Prediction scoring / persistence untouched |

### Persistence

| Item | Status | Evidence |
|------|--------|----------|
| Banter history persisted | PASS | `BanterHistoryService.RecordAsync` → `banter_content_history` |
| Provider content ID stored | PASS | `BanterContentHistory.ProviderContentId` |
| Scenario stored | PASS | `ScenarioType` |
| Search/concept phrase stored | PASS | `SearchPhrase` |
| Meme template ID stored where applicable | PASS | `MemeTemplateId` column + record path (null until Phase 4) |
| Usage timestamp stored | PASS | `UsedAtUtc` |
| Migration additive and safe | PASS | `20260902211014_AddBanterContentHistory` CreateTable only |

### Repetition prevention

| Item | Status | Evidence |
|------|--------|----------|
| Same recently used GIF excluded | PASS | `BanterHistoryService` + tests |
| Same recently used template excluded | PASS | `MemeTemplateIds` + tests |
| Recent search concepts avoided | PASS | Exclusion context + `OpenAiBanterConceptGenerator.Normalize` / prompt avoid-list |
| Exclusion windows configurable | PASS | `RecentContentWindowDays`, team, global |
| Empty candidate pools safe fallback | PASS | `BanterOrchestratorFallbackTests` |

### Scenario engine

| Item | Status | Evidence |
|------|--------|----------|
| Deterministic scenario classification | PASS | `DeterministicBanterScenarioClassifier` |
| AI not the only classification mechanism | PASS | Classifier is deterministic-only |
| Scenario list extensible | PASS | `BanterScenario` enum + predefined map |
| Generic win/draw/loss fallbacks | PASS | `GenericWin` / `GenericDraw` / `GenericLoss` + predefined concepts |

### Concept generation

| Item | Status | Evidence |
|------|--------|----------|
| Multiple concepts per banter event | PASS | `ConceptCount` / `ConceptsUsedPerGeneration` |
| Structured model output | PASS | `response_format: json_object` + `ParseConcepts` |
| Concepts normalized | PASS | `Normalize` / `Clean` |
| Duplicate concepts removed | PASS | Phrase hash set in `Normalize` |
| Recent phrases passed as exclusions | PASS | Prompt avoid-list + normalize filter |
| Predefined fallback concepts | PASS | `PredefinedBanterConcepts` |

### Giphy / candidate pool

| Item | Status | Evidence |
|------|--------|----------|
| Multiple concepts searched | PASS | `SampleConcepts` + per-query fetch |
| Multiple results per concept | PASS | `CandidatesPerConcept` |
| Candidate pool deduplicated | PASS | `Dictionary` by `ProviderContentId` |
| Application controls final randomness | PASS | `WeightedBanterCandidateSelector` + `IBanterRandom` |
| Provider top result not always selected | PASS | Top-N weighted roll (tests) |

### Scoring and selection

| Item | Status | Evidence |
|------|--------|----------|
| Relevance score | PASS | `BanterCandidateScorer.ScoreRelevance` |
| Novelty score | PASS | `ScoreNovelty` |
| Freshness score exists or neutral/documented | PASS | Neutral `0.5` with inline comment |
| Popularity score exists or neutral/documented | PASS | Rank-based `ScorePopularity` |
| Weights configurable | PASS | `Banter:Weights` |
| Top-N pool configurable | PASS | `TopCandidatePoolSize` |
| Weighted random selection | PASS | `WeightedBanterCandidateSelector` |
| Randomness controllable in tests | PASS | `SeededBanterRandom` |

### Meme generation

| Item | Status | Evidence |
|------|--------|----------|
| Existing meme generation still works | PASS | `AiEndpoints` `/meme` still via `IContentGenerator` |
| Template mode: select template before caption | NOT APPLICABLE | Phase 4 deferred |
| Template usage in repetition history | PASS | Column + exclusion (ready for Phase 4) |

### Testing

| Item | Status | Evidence |
|------|--------|----------|
| Unit tests added | PASS | Strategy test classes listed above |
| Integration tests where supported | PASS | History via `TestDbContextFactory` |
| Feature flag paths tested | PASS | Flag ON + OFF |
| OpenAI failure tested | PASS | HTTP 500 → predefined |
| Giphy failure tested | PASS | Empty pool + throw → legacy |
| Duplicate suppression tested | PASS | Scorer / normalize / history phrase |
| DTO compatibility tested | PASS | Fallback asserts `gif`/`image` media type |
| Full solution builds | PASS | `dotnet build BanterApp.Api` — 0 errors |
| Full test suite passes | PASS | `BanterApp.Api.Tests` — **297 passed**, 0 failed (2026-09-02) |

### Observability

| Item | Status | Evidence |
|------|--------|----------|
| Scenario logging | PASS | `BanterScenarioClassified` |
| Candidate count logging | PASS | `BanterCandidatesFetched` `count=` |
| Exclusion count logging | PASS | `BanterCandidatesExcluded` |
| Final selection logging | PASS | `BanterCandidateSelected` |
| Fallback logging | PASS | `BanterFallbackUsed` |
| No secrets logged | PASS | API keys not in log templates; Giphy request URL not logged |

### Rollout safety

| Item | Status | Evidence |
|------|--------|----------|
| Default production behavior chosen/documented | PASS | Default `false` in `appsettings.json` + this report / current-state |
| Enable in test/staging first | PASS | Config-only toggle |
| Rollback is configuration change | PASS | `UseStrategyEngine=false` |
| DB changes backward-compatible with legacy | PASS | Additive history table |

### Checklist counts

| Status | Count |
|--------|------:|
| PASS | 62 |
| FAIL | 0 |
| NOT APPLICABLE | 1 |
| BLOCKED | 0 |

---

## Unresolved items

| Severity | Item | Notes |
|----------|------|-------|
| LOW | Classifier rarely/never emits `Comeback`, `BottledIt`, `RefereeControversy` | Enum + predefined exist; extend classifier when match event signals available |
| LOW | Freshness always `0.5` | Documented; improve if Giphy timestamps are parsed later |
| LOW | Public `/api/ai/*` still uses `IContentGenerator` | Intentional; Hangfire GIF path is the Strategy Engine insertion point |
| DEFERRED | Phase 4 meme templates | Kit: only if safe fit |
| DEFERRED | Phase 5 engagement analytics | Explicitly out of scope |

No CRITICAL / HIGH / MEDIUM in-scope failures remain after Phase D fixes (test coverage for flag ON, OpenAI HTTP failure, Giphy throw, search-phrase exclusion, media-type contract).

---

## Enable / rollback

```json
// Enable (local / staging)
"Banter": { "UseStrategyEngine": true }

// Rollback (production-safe)
"Banter": { "UseStrategyEngine": false }
```

Apply migration `20260902211014_AddBanterContentHistory` before relying on exclusion history in any environment.

---

## Test commands / outcomes

```powershell
dotnet build c:\banterapp\backend\BanterApp.Api\BanterApp.Api.csproj
# Build succeeded. 0 Warning(s). 0 Error(s).

dotnet test c:\banterapp\backend\BanterApp.Api.Tests\BanterApp.Api.Tests.csproj --filter "FullyQualifiedName~BanterHistory|FullyQualifiedName~BanterCandidate|FullyQualifiedName~BanterOrchestrator|FullyQualifiedName~FeatureFlaggedBanter|FullyQualifiedName~OpenAiBanter|FullyQualifiedName~WeightedBanter|FullyQualifiedName~DeterministicBanter|FullyQualifiedName~PremierLeagueMatchScope|FullyQualifiedName~WorldCupLegacyPurge"
# Passed: 43

dotnet test c:\banterapp\backend\BanterApp.Api.Tests\BanterApp.Api.Tests.csproj
# Passed: 297, Failed: 0
```

---

## Phase D fixes applied

1. `FeatureFlaggedBanterGeneratorTests` — added **Flag ON** strategy path assertion  
2. `OpenAiBanterConceptGeneratorTests` — added **OpenAI HTTP failure → predefined**  
3. `BanterOrchestratorFallbackTests` — added **Giphy provider throw → legacy** + media-type DTO check  
4. `BanterHistoryServiceTests` — added **recent search-phrase exclusion**
