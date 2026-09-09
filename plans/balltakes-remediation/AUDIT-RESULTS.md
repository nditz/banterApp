# Audit Results

**Date:** 2026-09-09  
**Scope:** Phase 0 — repository and architecture audit only. No production code was changed.  
**Method:** Full plan-package read, then inspection of frontend (`Next.js 16` App Router), backend (`.NET 9` Minimal APIs + EF Core + Hangfire), tests, config, and legacy residue. Production data state (live API keys, `sync_runs` rows, fixture counts) was **not** queried; operational claims below are code-derived and must be confirmed against Render/Supabase in Phase 1.

---

## Executive Summary

Ball Takes is already a Premier League prediction product with a real stack: Vercel frontend, Render API, Supabase Auth + Postgres, Hangfire sync jobs, pundit ingest, Studio comparison + script export, guest-first sessions, and a substantial admin console.

The intended loop — **Follow pundits → Predict → Football happens → Compare → Receipt → Banter → Studio → Export content pack** — is only partially implemented:

| Loop step | Status |
|---|---|
| Follow pundits | **MISSING** |
| Predict | **EXISTS AND REUSABLE** |
| Football happens | **EXISTS BUT NEEDS REFACTOR** (sync + mock calendar risk) |
| Compare | **PARTIAL** (Studio only) |
| Receipt | **MISSING** as data; **PARTIAL** as UI metaphor |
| Banter | **PARTIAL** |
| Studio | **EXISTS BUT NEEDS REFACTOR** |
| Export content pack | **PARTIAL** (plain scripts, not packs) |

The most likely reason current matchweek fixtures look missing, and standings look empty, is **not World Cup residue**. It is the sports pipeline: default `SportsData:Provider=mock` only seeds **matchweeks 1–2 (21–31 Aug 2026)**. Audit date is **9 Sep 2026**, so mock current-week resolution parks on stale unfinished MW1/MW2 rows and never creates MW3+. Live API-Football is optional; score/standings jobs can complete while writing little data.

Studio is already a first-class destination (desktop + mobile nav). Do not demote or rebuild it as a blank prompt. Extend the existing comparison + script workspace.

---

## Architecture

```
Browser
  └─► Vercel (Next.js 16.3.4 App Router — balltakes.com)
        ├─► Supabase Auth (email/password + OAuth)
        └─► /api-backend/* rewrite ──► Render Docker (.NET 9 API — api.balltakes.com)
              ├─► Supabase PostgreSQL (EF Core, session pooler)
              ├─► Validates Supabase JWT
              ├─► Hangfire recurring jobs (InMemory storage)
              ├─► Cloudflare Turnstile
              └─► API-Football / Sportmonks / Football-Data.org / OpenAI / YouTube / Giphy / RSS
```

| Layer | Implementation |
|---|---|
| Frontend | Next.js `16.3.4`, React `19.2.8`, Tailwind v4, shadcn/Radix, TanStack Query, `@supabase/ssr` |
| Backend | ASP.NET Core Minimal APIs, EF Core 9 + Npgsql, FluentValidation, Hangfire |
| Auth | Hybrid: Supabase JWT for registered users; custom anonymous session (`X-Anonymous-Id` + recovery key) |
| Product DB | **Single store:** EF `AppDbContext` on Supabase Postgres |
| Prisma | `frontend/prisma/schema.prisma` is an **empty shell** (no models). Dead dependency. |
| Frontend API client | `frontend/src/lib/api.ts` — `NEXT_PUBLIC_API_URL` or `/api-backend` |
| Config | `backend/BanterApp.Api/appsettings.json`, `DEPLOYMENT.md`, `.env.example` |

**Preserve:** this topology. Do not introduce a second product database, a custom auth rewrite, or a Pages Router migration.

---

## Route Inventory

### Public / consumer

| Path | Purpose | Auth | Studio feed? | Product role |
|---|---|---|---|---|
| `/` | Home: welcome tour, picks, banter feed, table, rankings | Guest OK | Indirect (welcome CTA) | Primary |
| `/matchweek` | Current matchweek fixture board | Guest OK | No | Primary (Predict) |
| `/table` | Premier League standings | Guest OK | No | Secondary / commodity |
| `/awards` | Season calls (tournament bonuses) | Session for picks | No | Keep, reframe as Season Calls |
| `/leagues` | Private leagues create/join | Session for mutate | No | Primary (retention) |
| `/leagues/join/[code]` | Invite landing | Session to join | No | Contextual |
| `/studio` | Comparison + script generation | Guest OK | **Yes** | Central |
| `/rules` | Scoring / how-to | Public | No | Secondary |
| `/predictions/history` | Past picks + reaction cards | Session preferred | No | Keep as Receipts/history |
| `/terms`, `/privacy` | Legal | Public | No | Required |
| `/auth/login`, `/auth/register` | Supabase auth | Public | No | Required |
| `/auth/callback`, `/auth/confirm` | OAuth / email confirm | Public | No | Required |

### Redirects (`next.config.ts`)

| From | To |
|---|---|
| `/brackets` | `/matchweek` (permanent) |
| `/bonuses`, `/predictions`, `/predictions/make`, `/predictions/best-player`, `/predictions/top-scorer`, `/predictions/top-assists` | `/awards` |

### Admin (`/admin/*`, `robots: noindex`, cookie + `isPlatformAdmin`)

Overview, jobs (+ run detail), errors, sources, source-items, review, stats, football-data (countries/players/leaderboards), health, launch-checklist.

### Navigation

| Surface | Links |
|---|---|
| Desktop `AppShell` | Home, Matchweek, Table, Awards, Leagues, Studio, Rules, History |
| Mobile bottom | Home, Picks, Table, Leagues, Studio — **no Awards / History / Banter** |
| Home quick nav | Predictions, Matchweek, Table, Awards, Leagues, Studio |

**Plan vs code:** plan wants Predict / Banter / Studio / Leagues / Profile. Current nav keeps Table as a primary mobile item and omits Banter as a destination (banter lives on home). Studio is already present and should stay central.

### Layouts / proxy

- Root `layout.tsx` — fonts, theme, QueryProvider, `AppShell`, JSON-LD
- `proxy.ts` — Next 16 request interception; protects `/admin`; refreshes Supabase session
- No `frontend/src/app/api/**` BFF routes

---

## API / Backend Inventory

Registration: `Program.cs` maps 17 feature endpoint files.

**Auth legend:** Public / Session (user or anonymous + terms) / JWT / Admin.

| Group | File | Notable paths |
|---|---|---|
| Health | `HealthEndpoints.cs` | `GET /health`, `GET /api/health` |
| Matches | `MatchEndpoints.cs` | `/api/matches/*`, `/api/matchweeks/current`, `/api/standings` |
| Predictions | `PredictionEndpoints.cs` | create / update / history |
| Season awards | `TournamentBonusEndpoints.cs` | `/api/tournament-bonuses/*` |
| User predictions | `UserPredictionEndpoints.cs` | JWT season-style typed preds (**parallel / likely obsolete vs awards**) |
| Football ref | `FootballReferenceEndpoints.cs` | countries, players, top scorers/assists |
| Leagues | `LeagueEndpoints.cs` | create / join / standings |
| Leaderboards | `LeaderboardEndpoints.cs` | global, league, pundits; **friends + default leagues are mock** |
| Feed | `FeedEndpoints.cs` | `/api/feed/`, trending; reactions **in-memory** |
| Studio | `StudioEndpoints.cs` | `GET /api/studio/comparison` |
| Opinions | `OpinionEndpoints.cs` | sources, pundits, opinions, pundit predictions |
| AI | `AiEndpoints.cs` | analyze, banter, meme, video-script, broadcast-script, pundit-script |
| Auth/session | `AuthEndpoints.cs`, `SessionEndpoints.cs` | register/login/me + session/consent/recover/sync |
| Client errors | `ClientErrorEndpoints.cs` | `POST /api/errors/client` |
| Sync | `SyncEndpoints.cs` | Admin trigger/status |
| Admin | `AdminEndpoints.cs` | jobs, errors, sources, review, football-data, health, backfills |

Hangfire dashboard: `GET /hangfire` (admin filter).

---

## Database / Entity Inventory

Canonical store: `backend/BanterApp.Api/Data/AppDbContext.cs` → `public` Postgres.

### Football

`Competition`, `CompetitionSeason`, `Matchweek`, `Match`, `ClubTeam`, `SeasonTeam`, `StandingRow`, `MatchEvent`, `LineupPlayer`, `ExternalId`, `Country` (still has `FifaRanking`), `Player`, `PlayerStat`.

Hardcoded PL catalog: `PremierLeagueCatalog.cs` — league **39**, season **2026/27**, competition id `…000039`.

### Predictions / scoring

`Prediction` (Result / CorrectScore / DoubleChance, `PointsAwarded`, optional `LockedAt`)  
`MatchweekBonus`  
`TournamentBonusCategory` / `TournamentBonusPick` / `TournamentAwardResult`  
`UserPrediction` (parallel season API)  
`PredictionAggregate`

**No `Receipt` entity. No follow-pundit entity. No Aura table.**

### Pundits / media / content

`Pundit` (`Persona` vs `Source`, attribution, parody fields)  
`PunditPrediction` (match-linked structured pick + source URL/snippet/confidence)  
`PunditOpinion`  
`MediaSource`, `MediaItem`, `RssFeed`, `NewsFeedItem`  
`GeneratedContent` (Analyze/Banter/Meme/VideoScript)  
`BanterContentHistory`, `ReactionGifUse`

### Identity / social

`User`, `AnonymousUser`, `League`, `LeagueMember`

### Ops

`SyncRun`, `SyncError`, `JobRegistryState`, `ProviderUsageDaily`, `OperationalError`, `IngestionError`, `ApplicationErrorLog`, `AdminAuditLog`, `AuthAuditLog`, `AppMetric` (**schema only, unused writers**)

### Migrations of note

| Migration | Relevance |
|---|---|
| `20260822113624_PremierLeagueRefocus` | Drops `bracket_picks`, wipes WC fixtures, seeds PL |
| `20260823211606_EnablePublicRowLevelSecurity` | RLS on all `public` tables, **no policies** (deny PostgREST; API bypasses) |
| `20260901125602_ReactionGifUses` | GIF uniqueness ledger |
| `20260902211014_AddBanterContentHistory` | Studio/banter anti-repeat |

---

## Background Jobs

Registered in `HangfireJobRegistration.cs` when `BackgroundJobs:Enabled`. Startup **triggers** score-sync, standings-sync, football-players, news, RSS, media, AI reactions, feed banter, RSS opinions, enrich, pundit extraction.

| Hangfire id | Class | Default cadence | Notes |
|---|---|---|---|
| `score-sync` | `ScoreSyncJob` | every 15 min | Fixtures + scores + compute standings + rescore |
| `match-details-sync` | `MatchDetailsSyncJob` | 15 min | Only `apifb-*` ids; mock `pl26-*` never enriched |
| `standings-sync` | `StandingsSyncJob` | ~360 min | Provider standings or compute from DB |
| `ai-reactions` | `AiReactionJob` | ~20 min | |
| `news-ingest` | `NewsIngestJob` | ~120 min | |
| `rss.feed.resolve` / `rss.sync` | RSS jobs | 360 / 20 min | |
| `youtube.metadata.sync` / `youtube.search.sync` / `youtube.transcript.sync` | Media + opinions | 360 / 180 / 10 min | |
| `openai.banter.generate` | `FeedBanterEnrichmentJob` | ~15 min | |
| `openai.opinion.extract` | `PunditExtractionJob` | ~10 min | |
| `predictions.aggregate.refresh` | `PredictionAggregateJob` | **Never** (manual) | |
| Football reference jobs | countries/players/stats/scorers/assists | daily / 30 min | |
| `football.reference_data.full_sync` | | **Never** | |

### Silent failure modes (P0)

1. `ScoreSyncJob` / `StandingsSyncJob` **catch exceptions, `FailAsync` on `sync_runs`, do not rethrow** → Hangfire can look successful.
2. `[AutomaticRetry(..., OnAttemptsExceeded = Delete)]` removes exhausted failures from Hangfire’s failed list.
3. Provider layers return **mock/empty** on API failure — job “completes”.
4. Admin `NextRunAt` is always `null` in `JobRegistryService.MapJob`.
5. `job_registry_state` pause/disable skips registration.

There is **no separate matchweek sync job**. Matchweeks are created during score sync via `CompetitionCatalogService.EnsureMatchweekAsync`.

---

## External Providers

| Provider | Config | Default / risk |
|---|---|---|
| **API-Football** | `SportsData:*` LeagueId 39, Season 2026 | **`Provider: mock`** unless env sets `apifootball` |
| **Mock** | `MockSportsDataProvider` | **MW1–2 only**, kickoffs 21–31 Aug 2026 |
| Sportmonks | `Sportmonks:*` | Token empty, LeagueId/SeasonId **0** → refuses unscoped |
| Football-Data.org | `FootballData:*` | Token empty |
| Football reference | `FootballReferenceData:*` `api_sports` | Scorers/assists/players |
| YouTube | `YouTube:*` | Key required in production validator |
| RSS | `config/rss-feed-catalog.json` + `football-banter.config.json` | |
| Giphy | `ReactionGif:*` | Empty API key → degraded GIF path |
| OpenAI | `Ai:*` | Default **`Provider: stub`** |
| News | `News:*` | |

Current competition/season: **hardcoded** `PremierLeagueCatalog`, not discovered.  
Current matchweek: `CurrentMatchweek.Resolve` — lowest unfinished week that is live or has future kickoff; else max unfinished; else max numbered week. Used by `GET /api/matchweeks/current`.

---

## Prediction Flow

**Status: EXISTS AND REUSABLE** (match + season awards)

### Match picks

- Frontend: `MatchweekBoard`, `PredictionCenter`, `MatchCard`, `PredictionButtons`, `ScoreCounter`, history page
- Backend: `Prediction` + `PredictionEndpoints` + `MatchLockService` (lock if live/FT or kickoff ≤ now) + `ScoringService` (3 / 7 / 2 pts + perfect MW +5) + `PredictionRescoreService` from `ScoreSyncJob`

Types: **Result, CorrectScore, DoubleChance**.

### Season calls

Active path: `/awards` → `TournamentBonusBoard` → `/api/tournament-bonuses` (winner, top 4, relegation, Golden Boot, etc.).

Parallel path: `UserPrediction` + `useUserPredictions` + orphaned `PlayerSelector` / `CountrySelector` — **do not extend; consolidate or delete**.

### Lifecycle vs plan (`draft → saved → locked → live → final → settled → receipt`)

| Intended | Actual |
|---|---|
| Draft | **MISSING** — create writes immediately |
| Saved | Row in `predictions` |
| Locked | Implicit via `MatchLockService`; `LockedAt` not systematically set at kickoff |
| Live / final | `Match.Status` |
| Settled | `PointsAwarded` rewritten; **no settlement ledger / event** |
| Receipt | **MISSING** as entity |

---

## Pundit Flow

**Status: PARTIAL** (ingest + structure reusable; follow + product UX missing)

Reusable: `Pundit`, `PunditPrediction` (match id, type, team, score, confidence, source URL/snippet), `PunditOpinion`, `PunditDisplayResolver`, RSS/YouTube/OpenAI extract jobs, admin review queue, `GET /api/pundits`, `GET /api/predictions/pundits`, Studio comparison of **Source** pundit picks.

Missing:

- Follow / unfollow graph
- Dedicated pundit browse/follow page
- Comparison on matchweek / post-match (only Studio)
- Studio tab copy still says “fictional pundit desk personas” even though comparison loads `PunditKind.Source`

Pundit data **is** structured enough for comparison **when** `PunditPrediction` rows are match-linked and reviewed. Opinions are weaker as comparable picks unless `MatchId` + structured `Prediction` are filled.

---

## Receipt / Story Flow

**Status: MISSING** (persistent receipt + story engine) / **PARTIAL** (UI cards)

There is no `Receipt` DbSet. Closest artifacts:

- `PredictionReceiptCard` — ephemeral share card after save
- `PredictionReactionCard` — history presentation
- Personalized feed copy that talks about “receipts”
- `GeneratedContent` — AI output log, not take↔outcome

Settlement (`PredictionRescoreService`) updates points only. It does **not** emit story candidates, receipt rows, or Studio inputs.

Story types from the plan (user beat pundit, majority wrong, derby, rank swing, etc.) are **not modeled**. Banter scenario classification exists inside `BanterOrchestrator` for GIF strategy only.

---

## Banter / Giphy / OpenAI

**Status: PARTIAL** (engines reusable; homepage timeline not product-complete)

| Piece | Status |
|---|---|
| Homepage `BanterFeedPanel` | LocalStorage banter + `/api/feed` (personal picks **or** pundit opinions + news) |
| `BanterOrchestrator` + history exclusions | **EXISTS AND REUSABLE** |
| `ReactionGifUse` uniqueness | **EXISTS AND REUSABLE** |
| Giphy provider | **EXISTS AND REUSABLE** (needs API key) |
| `/api/ai/banter` | Backend exists; homepage does not drive a receipt→banter product step |
| Guest empty feed fallback | Weak if ingest/news empty |
| Designed public GIF/meme timeline | **PARTIAL** — depends on jobs; local feed is not shared |

Homepage welcome is still a **long autoplay quick tour** (`HomeWelcomePanel` + `HOME_WELCOME_SLIDES`), which the plan wants reduced in favor of demonstrating real content.

---

## Studio

**Status: EXISTS BUT NEEDS REFACTOR** — keep, do not replace.

Current Studio (`StudioPage.tsx` + `useStudio` + `StudioEndpoints`):

- Tabs: My Picks / vs League / vs Pundits / Script
- Comparison DTO from user predictions + league + source pundit picks + FT result
- `CumulativeScriptExport` → `/api/ai/broadcast-script` (copy / download `.txt`)
- `PunditScriptGenerator` → `/api/ai/pundit-script`
- Video tab: **“coming soon”**
- Not a blank chat prompt
- Not the planned story-driven workspace (no receipt picker, trending stories, league drama, previous projects, content-type/tone, structured pack)

| Planned Studio input | Available today |
|---|---|
| User predictions | Yes |
| Pundit picks | Partial (Source predictions in comparison) |
| Match results | Yes when FT |
| Receipts | No |
| RSS / YouTube | Data exists elsewhere, not in Studio |
| Season calls / Aura / league drama | League rank summary only |
| Stats | Broadcast composer can pull match stats |

Sitemap currently gives `/studio` priority **0.5** — too low for a central feature.

---

## Auth / Admin

**Status: EXISTS AND REUSABLE**

- Supabase Auth (browser) + API JWT validation
- Anonymous guest play with cookie/localStorage id + recovery session key
- Terms gate (`TermsGate` / `POST /consent`) — **not** GDPR advertising consent
- Admin allowlist emails / user ids + `User.IsPlatformAdmin`
- Rich admin: jobs, errors, sources, pundit review, football-data, health, launch checklist
- RLS enabled on all `public` tables with **no policies** → PostgREST cannot read; API role bypasses. This is intentional, not a missing product feature.

Do not implement custom authentication. Reuse this hybrid.

---

## Ads / Consent

**Status: PARTIAL** (AdSlot exists) / **MISSING** (CMP) / **P0 for EEA**

- `AdSlot` + `ads.ts` + `AdSenseLoader` + `PageWithSideAds` already exist
- Publisher id hardcoded `ca-pub-5886846159925642`; slot IDs only from env
- **If slot env vars are unset, `AdSlot` returns `null`** — but `PageWithSideAds` still reserves sticky empty rails (`min-h-[calc(100vh-3.5rem)]`). That is why placeholders look like dead columns.
- Only `feed-0` can map from `NEXT_PUBLIC_ADSENSE_SLOT_FEED`; `feed-1+` never resolve
- **No no-fill collapse** once a live unit is mounted (`min-h` remains)
- **No CMP / cookie banner / `consent.ts`**. AdSense script loads `afterInteractive` whenever client id is non-empty
- Docs (`SETTINGS-INVENTORY-PRIVATE.md`) describe consent that **does not exist in code**
- Auto Ads (loader) can run even when manual slots are null → duplicate inventory once slots are wired

---

## Analytics / Observability

| System | Status |
|---|---|
| Error tracking | **EXISTS AND REUSABLE** — `ErrorTrackingService`, client error POST, admin errors |
| Sync runs | **EXISTS AND REUSABLE** — `SyncRun` / admin jobs |
| Admin health / launch checklist | **EXISTS AND REUSABLE** |
| `AppMetric` | Schema only, unused |
| Product funnel (DAU, Studio opens, copy/export) | Admin stats hardcodes `available = false` |
| Google Analytics / Vercel Analytics | **Not present** |

Ops observability is stronger than the plan implies. Product analytics is missing. Extend admin rather than building a second dashboard.

---

## World Cup Residue

Live SEO/routes are Premier League. Residue is naming, docs, seed, and defense-in-depth.

| Item | Classification |
|---|---|
| `WorldCupLegacyPurge` + `PremierLeagueMatchScope` WC filters | **KEEP** |
| `WorldCupLegacyPurgeTests` | **KEEP** |
| `/brackets` → `/matchweek` redirect | **KEEP** |
| `TournamentBonus*` tables/API/UI | **GENERALISE** → Season Calls (do not rebuild scoring) |
| `Country.FifaRanking` | **GENERALISE** (optional field) |
| `frontend/src/lib/team-flags.ts` FIFA map | **REPLACE FOR PREMIER LEAGUE** (club crests already exist via `club-badges.ts`; `TeamFlag` still used by table) |
| `components/brackets/TeamFlag.tsx` | **GENERALISE** (keep component, rename folder later) |
| Orphaned `CountrySelector` / `PlayerSelector` | **DELETE** or merge into awards |
| `docs/worldcup-edgy-reactions-pack/**` | **DELETE** / archive (duplicate of `frontend/src/reactions`) |
| `docs/BRACKETS.md`, `docs/WORLD_CUP_*` | **DELETE** / archive |
| `supabase/seed.sql` WC2026 group fixtures | **REPLACE FOR PREMIER LEAGUE** |
| `supabase/migrations/*brackets*` | **KEEP** as history |
| EF WC migrations | **KEEP** (immutable) |
| Golden Ball as product logic | **Not found** |
| Job copy “national teams/countries” | **GENERALISE** copy |

World Cup is **not** the primary cause of empty PL fixtures.

---

## SEO / Metadata

Live metadata is PL-oriented (`seo.config.ts`, home/matchweek/awards titles, JSON-LD). No World Cup strings under `frontend/src/app`.

| Asset | Status |
|---|---|
| `sitemap.ts` | PL routes; Studio priority 0.5 (raise later) |
| `robots.ts` | Disallows `/admin/`, `/api-backend/`, `/auth/`, `/predictions/` |
| OG image | `public/opengraph-image.jpg` |
| Canonicals | Present on key pages |

Stale WC copy lives in **docs/seeds**, not route metadata.

---

## Tests

### Backend (`BanterApp.Api.Tests`) — strong

xUnit + WebApplicationFactory. Coverage includes current matchweek, PL scope, scoring, lock, match APIs, tournament bonuses, leagues, football reference, feed/GIF/banter orchestrator, admin/jobs/errors, CSRF/rate-limit/SSRF/XSS, World Cup purge.

### Frontend — thin

- Vitest: `matchweek`, `auth-redirect`, `avatars`, `club-badges`, `avatar-image` only
- Playwright: responsive overflow + admin redirect smoke
- **No** e2e for predict → settle → Studio
- Almost no component/hook tests

### Gaps vs `15-TEST-PLAN.md`

Missing: settlement idempotency ledger tests, receipt generation, pundit comparison product tests, AdSlot consent, Studio context assembly, provider-unavailable frontend states.

---

## Security / Privacy

Present and reusable: CSRF double-submit, Turnstile on auth/consent/prediction writes, rate limits, security headers (API + Next CSP), production startup validator, secret sanitizer, admin Hangfire filter.

Concerns:

1. **AdSense without marketing consent** (GDPR/ePrivacy) — P0 for EEA.
2. Anonymous id cookie is forgeable; recovery token is the real guest secret.
3. `GET /api/predictions/aggregates` is **public** aggregated popularity — not individual pick sheets, but review before public receipts.
4. Feed reactions are in-memory (not a security hole, but incomplete).
5. `Supabase:ServiceRoleKey` declared, unused in `SupabaseAuthService` (anon key only) — keep off the frontend.
6. Individual prediction history is session-scoped. Public timeline must not leak private picks (feed currently uses personal cards for the **current** user only).

No committed production secrets found in templates.

---

## Reusable Components / Services

**Do not rebuild these. Extend them.**

1. `Prediction` + `ScoringService` + `MatchLockService` + `PredictionRescoreService`
2. `CurrentMatchweek.Resolve` + `MatchEndpoints` + `PremierLeagueMatchScope`
3. `TournamentBonus*` season awards (reframe UX/naming only)
4. `Pundit` / `PunditPrediction` / `PunditOpinion` + attribution + ingest + admin review
5. `StudioEndpoints` comparison DTO + `StudioPage` tabs + script export UX
6. `BanterOrchestrator` + `BanterContentHistory` + `ReactionGifUse`
7. League create/join/standings
8. `AdSlot` abstraction (fix behavior; don’t replace)
9. Admin jobs/errors/health/football-data consoles
10. Guest session + Supabase auth hybrid
11. `ErrorTrackingService` + client error reporter
12. Shared UI: `Button`, `Panel`, `Card`, `Skeleton`, `Dialog`, `Tabs`, `MatchCard`, `UserAvatar`

---

## Gap Matrix

| Area | Status | Severity | Existing implementation | Gap | Recommendation |
|---|---|---|---|---|---|
| Fixtures / current matchweek | EXISTS BUT NEEDS REFACTOR | P0 | `CurrentMatchweek`, `ScoreSyncJob`, mock MW1–2 | Mock calendar ends 31 Aug 2026; live provider optional; empty vs error mixed | Confirm `apifootball` in prod; expand mock or fail loudly; distinguish error/empty/stale |
| Standings | PARTIAL | P0 | `StandingsSyncJob`, `PremierLeagueStandingsCalculator`, `LeagueTable` | Empty until played>0; error not distinguished from empty | Surface sync failure; never fake “all good” |
| Football jobs observability | EXISTS BUT NEEDS REFACTOR | P0 | Hangfire + `sync_runs` + admin jobs | Exceptions swallowed; Hangfire Delete; `NextRunAt` null | Rethrow after `FailAsync`; show last error/next run |
| Match predictions | EXISTS AND REUSABLE | — | Result / CS / DC + scoring | No draft/receipt states | Keep scoring; add settlement events later |
| Season calls | EXISTS AND REUSABLE | P2 | `/awards` + TournamentBonus | WC naming; dual `UserPrediction` API | Reframe copy; freeze/remove unused API |
| Follow pundits | MISSING | P1 | Pundit entities + APIs | No follow graph/UX | Add follow on existing `Pundit` |
| Pundit comparison | PARTIAL | P1 | Studio comparison | Not on matchweek/post-match; personas vs Source copy | Reuse DTO; expose on match cards |
| Persistent receipts | MISSING | P1 | UI receipt cards | No entity; settlement doesn’t emit | New table from settlement, feed Studio |
| Story engine | MISSING | P2 | Banter scenario classifier | No story types/ranking | Build on orchestrator, don’t fork |
| Homepage banter timeline | PARTIAL | P1 | Feed + Giphy + local banter | Empty/job-dependent; long welcome tour | Designed fallbacks; shorten tour |
| Studio workspace | EXISTS BUT NEEDS REFACTOR | P1 | Comparison + scripts | No story picker / packs / history UI | Evolve in place |
| Content packs / export | PARTIAL | P1 | Copy/download text | No structured pack JSON | Extend `GeneratedContent` |
| Aura | PARTIAL | P2 | Client `localStorage`; backend **points** | Aura not durable; competes with points | Keep points canonical; Aura as UX layer over points |
| Leagues | EXISTS AND REUSABLE | P2 | Full CRUD + standings | No rivalry receipts / recaps | Add stories later |
| Retention hooks | PARTIAL | P2 | Open fixture count | No unfinished nudges/recaps | After receipts exist |
| AdSlot | EXISTS BUT NEEDS REFACTOR | P0 (consent) / P1 (fill) | `AdSlot` + rails | No CMP; empty rails; no no-fill | Gate loader; collapse rails; map slots |
| Product analytics | MISSING | P2 | `AppMetric` unused | Funnel “Not wired” | Wire writers or hide admin cards |
| WC residue | PARTIAL | P2 | Purge + PL refocus | Naming, docs, `seed.sql`, team-flags | Cleanup without touching scoring |
| Dual prediction APIs | OBSOLETE | P2 | UserPrediction vs TournamentBonus vs match Prediction | Confusion | Awards path wins |
| Frontend tests | PARTIAL | P1 | Few unit + responsive e2e | No critical-path e2e | Add predict/settle/Studio tests as features land |
| Mock leaderboards | PARTIAL | P2 | Friends / default leagues mock | Misleading UI | Real or hide |
| Prisma | OBSOLETE | P3 | Empty schema | Dead dep | Remove when convenient |
| Design system | PARTIAL | P3 | `ui/*` + tokens | Admin zinc palette; one-off Studio cards | Phase 10 only |
| Mobile nav vs plan | PARTIAL | P2 | Studio already in bar | Table primary; no Banter/Awards | Phase 2 IA |

---

## Acceptance criteria vs repository

Compared to `16-ACCEPTANCE-CRITERIA.md`:

| Criterion | Now |
|---|---|
| Homepage communicates predict → pundit → receipts → Studio | **Partial** — predict + Studio CTAs exist; receipts/pundit comparison not first-class on home |
| Public banter timeline real/fallback | **Partial** — feed exists; empty/job-dependent; local fallback not product-grade |
| Studio central and story-populated | **Partial** — central in nav; not story-driven |
| Follow pundits or equivalent exposed | **Fail** — missing |
| User vs pundit comparison visible | **Partial** — Studio only |
| Receipts persistent and reusable | **Fail** |
| Current matchweek / fixtures / settlement / standings | **At risk** — code exists; mock/live sync likely stale after 31 Aug 2026 |
| Jobs observable in admin | **Partial** — UI exists; Hangfire can hide failures |
| Ad placeholders collapse | **Fail** — rails reserved; no no-fill |
| Consent respected | **Fail** for ads |
| No stale WC primary UI | **Pass** on live routes; residue in naming/docs/seed |
| Mobile nav coherent | **Partial** |
| Loading/empty/error designed | **Partial** — several empty/error conflations (`MatchweekBoard` error copy: “Official 2026/27 fixtures shown”) |
| Receipt → content pack e2e | **Fail** |
| Architecture reused | **Pass** (this audit’s constraint) |
| Tests for changed critical paths | N/A until implementation |
| Implementation log | Not started (Phase 0) |

---

## Plan corrections (repository is right)

Do **not** follow the plan where working code already solves the requirement:

1. **Do not rebuild Studio** as a new app. Evolve `StudioPage` / `StudioEndpoints`.
2. **Do not invent a new scoring model.** `ScoringService` + tournament bonus scoring stay.
3. **Do not replace backend points with Aura.** Aura is cosmetic `localStorage`. Canonical currency is `PointsAwarded` / league standings. Treat Aura as a presentation layer unless a later phase explicitly migrates it server-side.
4. **Do not create a second football pipeline.** Harden `ScoreSyncJob` + API-Football.
5. **Do not rewrite `CurrentMatchweek`.** It is tested and matches BBC-style open-week behavior. Fix **data**, not the resolver, unless kickoff-null rows still pin stale weeks after live sync.
6. **Do not add a new AdSlot component.** Fix `AdSlot` + `PageWithSideAds` + consent.
7. **Do not add custom auth.** Supabase + anonymous session is the product.
8. **Do not rebuild admin observability from scratch.** Extend jobs/health/errors.
9. **Do not delete guest-first play** to force login.
10. **Season is 2026/27 on purpose** (`PremierLeagueCatalog`). Do not “fix” it to 2025/26.
11. **`TournamentBonus*` is the season-calls implementation**, not leftover WC gameplay. Rename in UX; keep tables until a dedicated rename migration is worth it.
12. **Banter novelty already exists** (`BanterContentHistory`, GIF ledger). Reuse for receipts/stories.
13. **Homepage already demonstrates product in three columns** (picks / banter / table). Shorten the welcome tour rather than replacing the home architecture.

---

## P0

1. Confirm production `SportsData__Provider=apifootball` + API key; inspect `sync_runs` and `matches` for MW≥3.
2. Stop treating mock MW1–2 as a full season; after 31 Aug 2026 current week cannot advance without live fixtures.
3. Distinguish frontend **error / empty / stale** for fixtures and standings (today: “Demo fixtures shown”, “Official fixtures shown”, “Table appears once synced”).
4. Make score/standings job failures visible in Hangfire (rethrow after `FailAsync`; stop deleting exhausted retries without admin signal).
5. Do not load AdSense before advertising consent (EEA). Gate `AdSenseLoader`.
6. Collapse unused ad rails when slot ids are missing or no-fill.

## P1

1. Follow-pundits model + UX on existing `Pundit`.
2. Persistent receipt emitted on settlement; Studio can open it.
3. Evolve Studio into story-driven workspace + structured content pack export (keep comparison/scripts).
4. Embed user-vs-pundit comparison on matchweek / results, not only Studio.
5. Homepage banter timeline fallbacks + shorten welcome tour.
6. AdSense slot mapping (`feed-*`) + no-fill handler.
7. Critical-path tests: matchweek resolve with live calendar, settlement, Studio comparison.
8. Fix Studio pundit copy (Source vs fictional personas).

## P2

1. Reframe Awards → Season Calls; hide Table from primary mobile if IA requires it.
2. Archive WC docs/pack; replace `supabase/seed.sql`; trim FIFA team-flags.
3. Remove or freeze `UserPrediction` / orphaned selectors.
4. Server Aura **or** stop presenting localStorage Aura as durable reputation.
5. League rivalry receipts / weekly recap hooks.
6. Wire `AppMetric` or remove “Not wired” admin cards.
7. Replace mock friends/default league leaderboards.
8. Raise Studio sitemap priority.

## P3

1. Design tokens / admin vs consumer visual unification.
2. Remove unused Prisma.
3. Direct third-party Studio export integrations (Canva, CapCut, etc.).
4. Video Studio (“coming soon”).
5. Draft prediction state.

---

## Recommended Phase 1

Phase 1 is **production integrity only** (see `IMPLEMENTATION-BACKLOG.md`). Do not start pundit follow, receipts, or Studio packs in the same run.

Highest-leverage order:

1. Verify live sports provider + job health against production (admin `/admin/jobs`, `/admin/health`, `sync_runs`).
2. If mock: either switch to API-Football or extend mock calendar so current matchweek is not stuck on Aug 2026.
3. Fail loudly on empty expected fixtures / standings (API + UI).
4. Make Hangfire/admin reflect score/standings failures.
5. Consent-gate AdSense; collapse empty rails.
6. Log remaining P0s that need production credentials the repo cannot prove.

**Stop.** Do not implement Phase 1 in this run.
