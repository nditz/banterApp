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
| P3-01 | Follow model | **Done (local)** | `PunditFollow` on existing `Pundit`. User or anon, unique, Source only. | `PunditFollow`, migration `AddPunditFollows` |
| P3-02 | Follow UX | **Done (local)** | `/pundits` browse/follow; overflow + Me. Follows filter Studio + pundit feed when any follows exist. | `PunditsDirectory`, `PunditFollowService`, `PersonalizedFeedService` |
| P3-03 | Matchweek comparison | **Done (local)** | Before/after on MatchCard via `GET /api/studio/comparison?matchIds=`. Reuses Studio DTO. | `StudioComparisonService`, `MatchPunditComparison` |
| P3-04 | Attribution | **Done** | Still `PunditDisplayResolver`. Source URL on directory + picks. No fabricated quotes. | resolver, Studio/directory UI |
| P3-05 | Ingest quality | **Done (local)** | Hide unreviewed/rejected from comparison. Approve backfills match-linked `PunditPrediction`. Health counts. Admin review stays. | `PunditMatchPredictionSync`, `AdminReviewService`, `AdminHealthService` |

**Depends on:** Phase 1 matches in prod (already true). Production **migration + API deploy** required before the follow graph exists live.

**Phase 3 gate:** follow + comparison + attribution in production. Start Phase 4 only after that verify pass.

**Out of scope:** receipts (Phase 4), Studio content packs (Phase 5), CMP, Aura persistence.

---

## Phase 4 - Receipt Engine

| ID | Item | Action |
|---|---|---|
| P4-01 | Receipt entity | Persistent structured receipt: user/anon, match, prediction, pundit take(s), result, points/Aura delta, provenance. |
| P4-02 | Settlement emit | `PredictionRescoreService` / score-sync creates receipts **idempotently** (one per prediction+result version). |
| P4-03 | History UI | Reframe `/predictions/history` as Receipts; reuse `PredictionReceiptCard` / `PredictionReactionCard`. |
| P4-04 | Story candidates | Classify events (beat pundit, exact score, miss, majority-wrong) using existing banter scenario pieces where possible. |
| P4-05 | Privacy | Public timeline never shows identifiable private receipts by default. |

**Depends on:** Phase 1 settlement actually running; Phase 3 if “vs pundit” receipts are required in the first receipt slice (can ship user-only receipts first).

---

## Phase 5 - Studio Evolution

| ID | Item | Action |
|---|---|---|
| P5-01 | Story workspace | Studio home sections: latest receipts, you vs pundits, trending, previous projects. **Extend `StudioPage`, do not replace.** |
| P5-02 | Content types + tone | Short/Reel, podcast, meme, caption, thread, carousel, commentary + tone chips. |
| P5-03 | Content pack | Structured output: title, hook, script, facts, visuals, captions, hashtags, AI prompts, source notes. Persist via `GeneratedContent` (extend types) or new pack table. |
| P5-04 | Export | Copy script / copy pack / download JSON or text (already have copy/download — extend). |
| P5-05 | Context assembly | Never invent structured football facts when `Match` / `Prediction` / receipt data exists. |
| P5-06 | Video “coming soon” | Hide or implement; do not leave a dead CTA. |

**Depends on:** Phase 4 receipts for the story picker. Comparison/scripts already exist.

---

## Phase 6 - Homepage Banter Timeline

| ID | Item | Action |
|---|---|---|
| P6-01 | Public mix | GIF / meme / pundit receipt / community highlight / match banter with designed fallbacks. |
| P6-02 | Novelty | Reuse `BanterContentHistory` + `ReactionGifUse`. |
| P6-03 | Personalization | Signed-in: unfinished picks, new receipts, followed pundits. Guest: public mix only. |
| P6-04 | LocalStorage feed | Do not treat `banter_local_feed` as the product timeline. |

**Depends on:** Phase 1 feed jobs having data; Phase 4 for receipt cards on the timeline.

---

## Phase 7 - Leagues & Aura

| ID | Item | Action |
|---|---|---|
| P7-01 | Aura vs points | Decide: Aura as UX label over server points, **or** persist Aura. Stop dual-truth `localStorage`. |
| P7-02 | Rank movement | Weekly delta on league standings; Studio/receipt hooks. |
| P7-03 | Rivalry receipts | League-specific stories from Phase 4 types. |
| P7-04 | Retention | Unfinished picks, weekly recap. |
| P7-05 | Mock boards | Replace `/api/leaderboards/friends` and default leagues mock with real or hidden. |

**Depends on:** Phase 4 events. Do not rename scoring rules.

---

## Phase 8 - Ads & Consent

| ID | Item | Action |
|---|---|---|
| P8-01 | CMP | GDPR/ePrivacy consent before AdSense/tracking. Terms consent ≠ ad consent. |
| P8-02 | Slot keys | Map `home-feed-*`, rails, matchweek, table per `11-ADSENSE-CONSENT.md`. Fix `feed-1+` mapping. |
| P8-03 | No-fill | Collapse live units that don’t fill; collapse rails (started in P1-08). |
| P8-04 | Auto vs manual | Avoid duplicate Auto Ads + manual units. |
| P8-05 | Placement analytics | Non-invasive fill/view metrics if CMP allows. |

**Depends on:** P1-07/P1-08 as a stopgap. Full CMP can complete here if Phase 1 only disabled ads.

---

## Phase 9 - Observability & Admin

| ID | Item | Action |
|---|---|---|
| P9-01 | Data-health | Counts + freshness for competitions, fixtures, standings, pundits, receipts, banter, Studio gens. Extend `/admin/health` + football-data. |
| P9-02 | Job UX | Last run, duration, processed, last error, next run, trigger (mostly exists — fill `NextRunAt` from Phase 1). |
| P9-03 | Product metrics | Wire `AppMetric` or remove “Not wired” cards: predict, return after result, receipt view, Studio open, generate, copy, league join, pundit follow. |
| P9-04 | Alerts | Missing current MW, zero fixtures when expected, settlement/generation failures. |

**Preserve:** existing admin console. Do not build a parallel ops app.

---

## Phase 10 - Visual Finishing

Only after Phases 1–6 core loops work.

| ID | Item |
|---|---|
| P10-01 | Shared primitives: EmptyState, ErrorState, ReceiptCard, PunditCard, StoryCard (reuse `Panel`/`Card`) |
| P10-02 | Token discipline; reduce one-off Studio/admin palettes |
| P10-03 | Mobile 375 / 768 / 1440; ad collapse; tables |
| P10-04 | Welcome/homepage hierarchy after content exists |
| P10-05 | Accessibility pass |

---

## Phase 11 - Final Verification

Use `cursor/VERIFY-PROMPT.md` + `cursor/COMPLETION-LOOP.md`. Repeat until `16-ACCEPTANCE-CRITERIA.md` passes. Do not mark complete with open P0/P1 in the current phase.

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
| Phase 4 | `receipts` (+ optional `receipt_story_candidates`) | FK to prediction/match/pundit; idempotency key on prediction id + result hash |
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
