# Implementation Backlog

Source: Phase 0 audit (`AUDIT-RESULTS.md`) mapped onto `17-IMPLEMENTATION-PHASES.md` and `16-ACCEPTANCE-CRITERIA.md`.

**Rule:** implement one phase per run. Preserve existing architecture. Do not rebuild working scoring, Studio, auth, or the football sync pipeline.

---

## Phase 1 - Production Integrity

P0 items that make the live product look empty or silently broken.

| ID | Item | Status today | Action | Files / services |
|---|---|---|---|---|
| P1-01 | Live sports provider | **Deployed honesty; live ingest still empty** | Production is `apifootball-live`. Latest score-sync **completed** with **0** items and did **not** mock-fill. DB still 20 `pl26-*`. football-data fallback is dropping `fd-*` rows (`Group` empty) until the local mapper ships. | Render env, `ApiFootballProvider`, `FootballDataFixtureMapper`, `ScoreSyncJob` |
| P1-02 | Current matchweek fixtures | **Stale visible in prod** | Envelope `status=stale`, `source=mock`, unofficial MW2. Do **not** rewrite `CurrentMatchweek.Resolve`. | `MatchEndpoints.GetCurrentMatchweek`, `CurrentMatchweek` |
| P1-03 | Mock calendar ceiling | **Done** | Mock calendar extended through MW4 (Sep 2026) and marked unofficial. Prod DB still MW1–2 only. | `MockSportsDataProvider.BuildFixtures` |
| P1-04 | Standings empty vs failed | **Done in prod** | Envelope `{ status, rows, error }` with `status=stale`. UI distinguishes empty / error / stale. | `GetStandings`, `LeagueTable` |
| P1-05 | Score/standings Hangfire silence | **Partial in prod** | Health still `degraded` (overdue). Latest score-sync is `completed` / 0 items / null error (weaker than previous `failed`). Admin jobs UI 401 without auth. | `ScoreSyncJob`, `StandingsSyncJob`, `JobRegistryService` |
| P1-06 | Frontend empty/error/stale | **Deployed; not click-verified** | Stopped silent mock fallback. Stale copy in production JS. | `MatchweekBoard`, `PredictionCenter`, `LeagueTable`, `useMatches`, `football-dataset.ts` |
| P1-07 | AdSense without consent | **Done (stopgap)** | Do not load `AdSenseLoader` / slots until advertising consent is granted. No CMP UI (Phase 8). | `AdSenseLoader`, `AdSlot`, `advertising-consent.ts` |
| P1-08 | Dead ad rails | **Done (not click-verified)** | Collapse `PageWithSideAds` when slots or consent are missing. | `PageWithSideAds`, `AdSlot` |
| P1-09 | Production data proof | **Done** | Re-verified 2026-09-09 after `main` `#38` / `b8ed852`. See `VERIFICATION-REPORT.md`. | Admin `/admin/jobs`, `/admin/health`, `/api/health` |

**Phase 1 gate:** integrity deploy **passed** (stale/error visible; no mock-fill). **Not complete:** live ingest still returns 0 PL fixtures (R2). Do not start Phase 2 until live `fd-*` / `apifb-*` MW≥3 exists **or** stale mock + failed/empty score-sync is explicitly accepted as the integrity end state.

**Out of scope:** follow pundits, receipts, Studio packs, nav IA, design polish.

---

## Phase 2 - Product Cleanup

| ID | Item | Action |
|---|---|---|
| P2-01 | Navigation IA | Align with Predict / Banter / Studio / Leagues / Me. Keep Studio central. Demote Table from mobile primary. Expose Awards as Season Calls (desktop + overflow). |
| P2-02 | Awards → Season Calls | UX/copy rename. Keep `TournamentBonus*` APIs until a later rename migration is justified. |
| P2-03 | Welcome tour | Shorten `HomeWelcomePanel` / `HOME_WELCOME_SLIDES`. Lead with live picks + feed. |
| P2-04 | WC docs/pack | Archive `docs/worldcup-edgy-reactions-pack`, `docs/BRACKETS.md`, `docs/WORLD_CUP_*`. |
| P2-05 | `supabase/seed.sql` | Replace WC2026 group fixtures with PL seed or document “do not use”. |
| P2-06 | Orphan prediction UI | Remove or merge `CountrySelector` / `PlayerSelector` / unused `UserPrediction` client path. |
| P2-07 | Studio sitemap | Raise `/studio` priority from 0.5. |
| P2-08 | Studio pundit copy | Stop saying “fictional pundit desk personas” on vs-pundits tab if Source predictions are shown. |

**Preserve:** `/brackets` redirect, `WorldCupLegacyPurge`, PL match scope filters.

---

## Phase 3 - Pundits & Comparison

| ID | Item | Action |
|---|---|---|
| P3-01 | Follow model | `PunditFollow` (user/anon → pundit). Do not invent a second pundit table. |
| P3-02 | Follow UX | Browse/follow on existing pundit list APIs; use follows to filter Studio + feed. |
| P3-03 | Matchweek comparison | Before/after: user pick vs followed Source `PunditPrediction`s. Reuse `StudioEndpoints` DTO shape. |
| P3-04 | Attribution | Keep `PunditDisplayResolver`; never fabricate quotes. |
| P3-05 | Ingest quality | Ensure extract jobs produce match-linked `PunditPrediction`s; admin review stays. |

**Depends on:** Phase 1 data actually containing matches (otherwise comparison is empty).

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
