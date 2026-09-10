# Implementation Backlog

Source: Phase 0 audit (`AUDIT-RESULTS.md`) mapped onto `17-IMPLEMENTATION-PHASES.md` and `16-ACCEPTANCE-CRITERIA.md`.

**Rule:** implement one phase per run. Preserve existing architecture. Do not rebuild working scoring, Studio, auth, or the football sync pipeline.

---

## Phase 1 - Production Integrity

P0 items that make the live product look empty or silently broken.

| ID | Item | Status today | Action | Files / services |
|---|---|---|---|---|
| P1-01 | Live sports provider | **Pass in prod** | Production is `football-data-live`. 380 `fd-*` matches; current week is official MW4. | Render env, `FootballDataProvider`, `FootballDataFixtureMapper`, `ScoreSyncJob` |
| P1-02 | Current matchweek fixtures | **Pass in prod** | Envelope `status=ok`, `source=database`, `official=true`, MW4. Do **not** rewrite `CurrentMatchweek.Resolve`. | `MatchEndpoints.GetCurrentMatchweek`, `CurrentMatchweek` |
| P1-03 | Mock calendar ceiling | **Done** | Mock calendar still in code for local/dev. Prod `pl26-*` rows purged via SQL Editor. | `MockSportsDataProvider.BuildFixtures`, `scripts/purge-leftover-pl26-mock.sql` |
| P1-04 | Standings empty vs failed | **Pass in prod** | Envelope `{ status, rows }` with `status=ok`, 20 clubs, Forest=`NOT`. | `GetStandings`, `LeagueTable`, `PremierLeagueStandingsCalculator` |
| P1-05 | Score/standings Hangfire silence | **Pass in prod** | Score-sync `completed`. Health `ok`; `overdueUnfinishedFixtures=false`. Admin jobs UI 401 without auth. | `ScoreSyncJob`, `StandingsSyncJob`, `JobRegistryService` |
| P1-06 | Frontend empty/error/stale | **Deployed; not click-verified** | Current week and standings APIs are `ok`. | `MatchweekBoard`, `PredictionCenter`, `LeagueTable`, `useMatches`, `football-dataset.ts` |
| P1-07 | AdSense without consent | **Changed in prod** | Ads load unless consent is `"denied"`. `adsbygoogle.js` is in production HTML. No first-party CMP UI (Phase 8). | `AdSenseLoader`, `AdSlot`, `advertising-consent.ts` |
| P1-08 | Dead ad rails | **Done (not click-verified)** | Default display slot `6603089832`; rails collapse only when ads are not allowed. | `PageWithSideAds`, `AdSlot` |
| P1-09 | Production data proof | **Done** | Re-verified 2026-09-09 after mock purge. See `VERIFICATION-REPORT.md`. | Admin `/admin/jobs`, `/admin/health`, `/api/health` |

**Phase 1 gate:** live ingest **passed**. Leftover `pl26-*` **purged**. P0/P1 football integrity **complete**. Start Phase 2 only in a new deliberate task.

**Out of scope:** follow pundits, receipts, Studio packs, nav IA, design polish.

---

## Phase 2 - Product Cleanup

UX/IA cleanup after Phase 1 football integrity. Studio stays central. No sports-pipeline or scoring changes.

| ID | Item | Status today | Action | Files / services |
|---|---|---|---|---|
| P2-01 | Navigation IA | **Done** | Desktop: Predict / Banter / Studio / Leagues / Season Calls + More. Mobile bottom: Predict / Banter / Studio / Leagues / Me. Table demoted to overflow. | `navigation.ts`, `AppShell`, `MobileBottomNav`, `HomeQuickNav`, `/me` |
| P2-02 | Awards → Season Calls | **Done** | User-facing copy/nav/title. URL remains `/awards`. `TournamentBonus*` APIs unchanged. | `awards/page.tsx`, `TournamentBonusBoard`, `rules`, `leagues`, `scoring-rules.ts` |
| P2-03 | Welcome tour | **Done** | Home carousel is 3 slides (picks → feed → Studio). Full tour remains on `/rules`. | `HOME_WELCOME_SLIDES`, `HomeWelcomePanel` |
| P2-04 | WC docs/pack | **Done** | Archived under `docs/_archive/world-cup/`. `/brackets` redirect kept. | `docs/_archive/world-cup/` |
| P2-05 | `supabase/seed.sql` | **Done** | Intentionally empty; documents do-not-use. Local mock / live sync own fixtures. | `supabase/seed.sql`, `supabase/README.md` |
| P2-06 | Orphan prediction UI | **Done (no route)** | No page imports. Unused files still on disk (`CountrySelector`, `PlayerSelector`, `useUserPredictions`). Backend `UserPrediction` APIs kept. | frontend predictions + hooks |
| P2-07 | Studio sitemap | **Done** | `/studio` priority **0.9** (same as matchweek). Table demoted to 0.6. | `sitemap-routes.ts` |
| P2-08 | Studio pundit copy | **Done** | vs-pundits tab: “sourced pundit predictions”. | `StudioPage` |

**Preserve:** `/brackets` redirect, `WorldCupLegacyPurge`, PL match scope filters.

**Phase 2 gate:** nav IA + Season Calls copy + shortened welcome + WC archive + empty seed + orphan UI gone + Studio sitemap/copy. Start Phase 3 only in a new deliberate task.

**Out of scope:** follow pundits, receipts, Studio packs, CMP, Aura persistence.

---

## Phase 3 - Pundits & Comparison

| ID | Item | Status today | Action | Files / services |
|---|---|---|---|---|
| P3-01 | Follow model | **Done** | `PunditFollow` on existing `Pundit`. User or anon, unique, Source only. | `PunditFollow`, migration `AddPunditFollows` |
| P3-02 | Follow UX | **Done** | `/pundits` browse/follow; overflow + Me. Directory loading uses `isPending`. | `PunditsDirectory`, `PunditFollowService`, `PersonalizedFeedService` |
| P3-03 | Matchweek comparison | **Done** | Guest `GET /api/studio/comparison?matchIds=` returns **200** in prod. | `StudioComparisonService`, `MatchPunditComparison` |
| P3-04 | Attribution | **Done** | Still `PunditDisplayResolver`. Source URL on directory + picks. No fabricated quotes. | resolver, Studio/directory UI |
| P3-05 | Ingest quality | **Done (local)** | Hide unreviewed/rejected from comparison. Approve backfills match-linked `PunditPrediction`. Health counts. Admin review stays. | `PunditMatchPredictionSync`, `AdminReviewService`, `AdminHealthService` |

**Depends on:** Phase 1 matches in prod (already true). Follow graph is live.

**Phase 3 gate:** **passed in production** (`#42`). Residual ingest mix is not a P0/P1.

**Out of scope:** Studio content packs (Phase 5), CMP, Aura persistence.

---

## Phase 4 - Receipt Engine

| ID | Item | Status today | Action | Files / services |
|---|---|---|---|---|
| P4-01 | Receipt entity | **Done** | `PredictionReceipt` on existing prediction/match. Owner XOR. Unique prediction + result hash. | `PredictionReceipt`, migration `AddPredictionReceipts` |
| P4-02 | Settlement emit | **Done** | `PredictionRescoreService` emits idempotently via `ReceiptSettlementService`. | `ReceiptSettlementService`, `ScoreSyncJob` (unchanged call site) |
| P4-03 | History UI | **Done** | `/predictions/history` is Receipts in production `#42`. | `predictions/history/page.tsx`, `useReceipts` |
| P4-04 | Story candidates | **Done** | Classified types + `receipt_story_candidates`. No invented quotes. | `ReceiptStoryClassifier` |
| P4-05 | Privacy | **Done** | `GET /api/receipts` is 401 without session. Public feed does not list receipt ids. | `ReceiptPrivacy`, `ReceiptEndpoints`, `PersonalizedFeedService` |

**Depends on:** Phase 1 settlement (already running). Vs-pundit snapshot included when reviewed Source picks exist.

**Phase 4 gate:** **passed in production** (`#42`). Start Phase 5 in a new implement run.

**Out of scope:** Studio content packs (Phase 5), CMP, Aura table.

---

## Phase 5 - Studio Evolution

| ID | Item | Status today | Action | Files / services |
|---|---|---|---|---|
| P5-01 | Story workspace | **Done (local)** | Studio home sections: latest receipts, you vs pundits, trending, previous projects. Extended `StudioPage`. | `StudioStoryService`, `StudioStoryWorkspace`, `StudioPage` |
| P5-02 | Content types + tone | **Done (local)** | Short/Reel, podcast, meme, caption, thread, carousel, commentary + tone chips. | `StudioContentCatalog`, `studio-pack.ts` |
| P5-03 | Content pack | **Done (local)** | Structured pack persisted as `GeneratedContentType.ContentPack` JSON (no new table). | `StudioPackService`, `generated_content` |
| P5-04 | Export | **Done (local)** | Copy script / copy prompt / copy pack / download JSON or text. | `StudioStoryWorkspace`, `formatStudioPackText` |
| P5-05 | Context assembly | **Done (local)** | Facts copied from receipt/match/prediction or sourced headline. No invented scoreline or pundit quote. | `StudioContextAssembler`, `StudioContentPackComposer` |
| P5-06 | Video “coming soon” | **Done (local)** | Dead CTA removed. Script tab still exports for external video tools. | `StudioPage` |

**Depends on:** Phase 4 receipts for the story picker. Comparison/scripts already exist.

**Phase 5 gate:** **local tests/build passed.** Production click-through of receipt → pack is the remaining verify step.

**Out of scope:** OpenAI rewrite of packs, third-party Canva/CapCut, homepage timeline (Phase 6).

---

## Phase 6 - Homepage Banter Timeline

| ID | Item | Status | Action |
|---|---|---|---|
| P6-01 | Public mix | **Done (local)** | GIF / meme / pundit receipt / community highlight / match banter with designed fallbacks. `CommunityFeedService` adds anonymised crowd cards; `RotateCardTypes` prevents same-type runs. |
| P6-02 | Novelty | **Done (local)** | Upstream `BanterHistoryService` + `ReactionGifLedger` still own generation novelty; `DedupeAndVaryMedia` stops repeated media inside one timeline response. |
| P6-03 | Personalization | **Done (local)** | Signed-in and anonymous identities both get personal cards (unfinished picks, receipt-coloured copy, followed pundits). Guests get the public mix. |
| P6-04 | LocalStorage feed | **Done (local)** | `lib/banterFeed.ts` and `BanterLine.tsx` deleted; the timeline is server-owned. |

**Depends on:** Phase 1 feed jobs having data; Phase 4 for receipt cards on the timeline.

---

## Phase 7 - Leagues & Aura

| ID | Item | Status | Action |
|---|---|---|---|
| P7-01 | Aura vs points | **Decided + done (local)** | Aura is a UX label over server points. No second currency, no client store. `GET /api/aura/me` is the single source; `lib/aura.ts` deleted. |
| P7-02 | Rank movement | **Done (local)** | `rank`, `previousRank`, `rankDelta`, `weeklyPoints` on standings and leaderboards, derived from a rolling 7-day cutoff rather than a snapshot table. |
| P7-03 | Rivalry receipts | **Partial** | Community crowd cards cover the "you vs the crowd" story; league-specific rivalry receipts still ride on the Phase 4 receipt types. |
| P7-04 | Retention | **Partial** | Unfinished-picks card is live on the timeline; a weekly recap digest is not built. |
| P7-05 | Mock boards | **Done (local)** | `/api/leaderboards/friends` aggregates the caller's leagues; `lib/mock-data.ts` deleted and demo-board messaging replaced with real empty/error states. |

**Depends on:** Phase 4 events. Do not rename scoring rules.

---

## Phase 8 - Ads & Consent

| ID | Item | Status | Action |
|---|---|---|---|
| P8-01 | CMP | **Done (local)** | First-party opt-in banner; nothing from Google loads until consent is granted. Terms acceptance is deliberately separate. Choice is editable at `/privacy`. |
| P8-02 | Slot keys | **Done (local)** | `AD_PLACEMENT_KEYS` covers feed, rails, matchweek, standings, table and display, each with its own env override. |
| P8-03 | No-fill | **Done (local)** | `AdSlot` watches `data-ad-status` and collapses on `unfilled`; rails collapse without consent. |
| P8-04 | Auto vs manual | **Config, not code** | Nothing in the app enables Auto Ads. Auto Ads must stay off in the AdSense dashboard for pages rendering manual units. |
| P8-05 | Placement analytics | **Done (local)** | Anonymous `ad_slot_filled` / `ad_slot_unfilled` / `ad_init_failed` counters, shown in `/admin/stats`. |

**Depends on:** P1-07/P1-08 as a stopgap. Full CMP can complete here if Phase 1 only disabled ads.

---

## Phase 9 - Observability & Admin

| ID | Item | Status | Action |
|---|---|---|---|
| P9-01 | Data-health | **Done (local)** | `/admin/health` now also reports receipts, Studio generations and ad-system status alongside the existing fixture/standings/pundit freshness. |
| P9-02 | Job UX | **Already covered** | `JobRegistryService` supplies last run, average duration, success/failure counts, last error and `NextRunAt`; `/admin/jobs` renders all of them plus manual triggers. |
| P9-03 | Product metrics | **Done (local)** | `AppMetric` wired through `ProductMetricService` + `POST /api/metrics/event`. All eight funnel events plus ad fill counters are recorded anonymously; "No data yet" only shows when nothing has ever been recorded. |
| P9-04 | Alerts | **Done (local)** | `AdminHealthService.BuildAlerts` covers missing current matchweek, overdue fixtures, receipt backlog, repeated job failures, critical errors and ad init failures. |

**Preserve:** existing admin console. Do not build a parallel ops app.

---

## Phase 10 - Visual Finishing

Only after Phases 1–6 core loops work.

| ID | Item | Status |
|---|---|---|
| P10-01 | Shared primitives: EmptyState, ErrorState, ReceiptCard, PunditCard, StoryCard (reuse `Panel`/`Card`) | **Done (local)** — `components/ui/states.tsx` + `section-header.tsx`, adopted across feed, matchweek, table, standings, leagues, pundits, Studio and receipts |
| P10-02 | Token discipline; reduce one-off Studio/admin palettes | **Audited — no change needed** — product surfaces already token-only; raw palette usage is confined to the deliberately zinc-themed admin console |
| P10-03 | Mobile 375 / 768 / 1440; ad collapse; tables | **Partial** — ad collapse and responsive classes are in place; breakpoint click-through not run (no browser tooling in the session) |
| P10-04 | Welcome/homepage hierarchy after content exists | **Done (local)** — timeline is server-backed and mixed; hierarchy follows compact hero → timeline → matchweek → comparison → Studio → leagues |
| P10-05 | Accessibility pass | **Partial** — alt text, roles, focus-visible and a global reduced-motion safety net verified in code; keyboard/contrast click-through outstanding |

---

## Phase 11 - Final Verification

Use `cursor/VERIFY-PROMPT.md` + `cursor/COMPLETION-LOOP.md`. Repeat until `16-ACCEPTANCE-CRITERIA.md` passes. Do not mark complete with open P0/P1 in the current phase.

**2026-09-10 local pass:** backend 424 tests, frontend 41 tests, ESLint clean, `tsc --noEmit` clean, Next.js production build and backend Release build (0 warnings) both succeeded. See `VERIFICATION-REPORT.md`. Production verification is still outstanding for Phases 5-10.

---

## Dependencies

```
P1 sports data + visible failures
  ├─► P3 pundit comparison (needs matches + pundit predictions)
  ├─► P4 receipts (needs settlement on real results)
  │     ├─► P5 Studio stories/packs
  │     ├─► P6 homepage receipt/GIF mix
  │     └─► P7 league rivalry / Aura stories
  └─► P2 IA cleanup (can overlap after P1 starts, but not before fixtures work)

P1-07/08 ads stopgap ─► P8 full CMP + slot matrix
P1 job visibility ─► P9 metrics (optional parallel after P1)
P10 only after P5/P6 content exists
```

**External credentials required for Phase 1:** API-Football key, (optional) Giphy, OpenAI not required to unstick fixtures.

---

## Migrations

| When | Change | Notes |
|---|---|---|
| Phase 1 | Avoid if possible | Prefer env + job/error handling. Mock fixture expansion is code, not schema. |
| Phase 3 | `pundit_follows` | `UserId`/`AnonymousUserId` + `PunditId`, unique, RLS-safe via API only |
| Phase 4 | `prediction_receipts` + `receipt_story_candidates` | FK to prediction/match; unique `(PredictionId, ResultHash)`; RLS on; **apply in prod** |
| Phase 5 | Extend `generated_content` types **or** `content_packs` | Prefer extend if JSON payload is enough |
| Phase 7 | Aura persistence **only if** product decision says so | Default: map Aura UI to server points; no new currency |
| Phase 8 | Consent log optional | Do not store unnecessary PII |
| Phase 2 | **No** required rename of `tournament_bonus_*` | Copy-only unless API clients can move |
| Never | Recreate Prisma models | Dead; do not revive as a second store |
| Never | Drop `WorldCupLegacyPurge` / PL scope filters | Defense in depth |

Hangfire storage is **in-memory** — job history is not durable across deploys. `sync_runs` / `job_registry_state` in Postgres are the durable ops record; Phase 1/9 should rely on those, not Hangfire’s dashboard alone.

---

## Deferred / Future

- Direct Canva / CapCut / Sora / ElevenLabs integrations (copy/export first).
- Studio video generation.
- Draft prediction state.
- Friends leaderboard (real social graph).
- Removing Prisma dependency.
- Renaming `TournamentBonus*` tables/routes.
- Multi-competition beyond Premier League.
- Vercel Analytics / GA (only behind CMP).

---

## Plan documents to treat as overridden

Recorded so later phases do not “fix” working code:

1. Studio is not an empty prompt page — it is a comparison + script workspace to **evolve**.
2. Aura is not the backend score.
3. Season awards already live at `/awards`.
4. Admin data health / jobs already exist.
5. AdSlot already exists.
6. Guest-first auth already exists.
7. 2026/27 PL catalog is intentional.
8. Banter novelty ledgers already exist.
