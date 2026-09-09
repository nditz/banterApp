# Verification Report

**Date:** 2026-09-09 (re-run after `main` update `#37` / `46ba2b2`)  
**Phase under test:** Phase 1 — Production Integrity  
**Method:** Completion loop (`cursor/COMPLETION-LOOP.md` + `cursor/VERIFY-PROMPT.md`). Runtime probes against `api.balltakes.com` / `balltakes.com`, Vercel production deployment for `balltakes`, local tests/lint, and inspection of football read paths, jobs, ads, and empty/error/stale states.  
**Browser:** No interactive click-through. Public HTML plus shipped JS chunks were inspected. Stale banners are client-rendered.

Phase 1 is **deployed**. Integrity failures that were silent are now **visible**. Phase 1 is **not marked complete** while live ingest still returns zero Premier League fixtures (current week remains mock MW2, open picks 0).

---

## Gate

| Question | Result |
|---|---|
| All current-phase P0/P1 passing in production? | **No** — R2 (live ingest) remains P0 |
| Mark Phase 1 complete? | **No** |
| Start Phase 2? | **No** |

Previous blocker **R1 (not deployed)** is **resolved**. `#37` is on `main` and Vercel production is `READY` (`dpl_HZ1XGsUZ6bdCFGKd4E9aqzq2FoGu`, sha `46ba2b2`).

---

## Local verification (this run)

| Check | Result |
|---|---|
| Backend tests | **307 passed** (0 failed) |
| Frontend Vitest | **21 passed** (0 failed) |
| ESLint / `tsc --noEmit` | Clean (lint completed; typecheck invoked on main) |
| Next.js production build | **Vercel production READY** on `46ba2b2` |

Code compiling / shipping is not treated as a product pass.

---

## Production runtime (2026-09-09, **post–Phase 1 deploy**)

### `GET https://api.balltakes.com/api/health`

```json
{
  "status": "degraded",
  "database": { "connected": true, "provider": "postgresql", "matchCount": 20, "newsCount": 42 },
  "sportsData": {
    "provider": "apifootball",
    "mode": "apifootball-live",
    "syncIntervalMinutes": 15,
    "lastScoreSyncStatus": "failed",
    "lastScoreSyncAt": "2026-09-09T13:01:21.076622+00:00",
    "lastScoreSyncError": "Score sync fetched 0 Premier League fixtures from the live sports provider.",
    "lastScoreSyncItems": 1,
    "overdueUnfinishedFixtures": true
  }
}
```

Health no longer reports `ok` over an overdue mock calendar.

### `GET /api/matchweeks/current`

- Envelope present: `status=stale`, `source=mock`, `official=false`
- `error`: “These fixtures are overdue without results. Score sync may be failing.”
- `number`: **2**
- 10 rows, all `pl26-mw2-*`, all `NS`, kickoffs 28–31 Aug 2026

### `GET /api/standings`

Envelope `{ status, source, lastSyncedAt, error, rows }` (not a bare array).

- `status=stale`, `source=computed`, 20 rows
- `error`: standings based on finished matches while some kickoffs are overdue
- Brighton 1st (1 game); Chelsea/Fulham `played: 0`

### `GET /api/matches/upcoming`

**0 rows.** Stored PL calendar exists, so the read path no longer live-substitutes the sports provider. Upcoming filters to kickoffs after `now−3h`; all remaining `NS` rows are in the past. Homepage open-picks count is therefore **0** for a real reason, not a silent mock swap.

### `GET /api/matches/results`

9 finished MW1 rows (`pl26-mw1-*`).

### Public site

- Vercel production: [balltakes #37](https://vercel.com/stanley-kamau-s-projects/balltakes/HZ1XGsUZ6bdCFGKd4E9aqzq2FoGu) `READY`, commit `46ba2b2`.
- `/` — title Premier League; “Open picks”; Studio present; **no** World Cup/FIFA copy; **no** `adsbygoogle.js` in HTML (`ca-pub-` only via `google-adsense-account` meta).
- `/matchweek` JS includes “sample fixtures for local/dev” and “overdue without results”.
- Ads consent helper `balltakes_advertising_consent` / `canRequestAds` is in shipped chunks. `adsbygoogle.js` is not injected without consent.
- `/studio` — Content Studio (not removed).
- Admin `/api/admin/jobs` and `/api/admin/health` return **401** without auth (expected). Job failure is visible on public `/api/health`.

---

## Phase 1 backlog vs evidence

| ID | Item | Production | Status |
|---|---|---|---|
| P1-01 | Live sports provider | `apifootball-live`; score-sync **failed** with 0 fixtures; did **not** mock-fill | **Pass (honesty)** / **P0 remain: no live rows** |
| P1-02 | Current matchweek | Envelope `stale` + unofficial mock MW2 | **Pass (visible)** / **Fail (not a real open week)** |
| P1-03 | Mock calendar MW4 | Code on main; prod DB still MW1–2 only | Done in code; unused in prod until empty DB seed |
| P1-04 | Standings empty vs failed | Envelope `stale` + 20 computed rows | **Pass** |
| P1-05 | Hangfire silence | `lastScoreSyncStatus=failed` + error text on `/api/health` | **Pass** (admin UI 401, not clicked) |
| P1-06 | Empty/error/stale UI | Stale copy in production JS; no demo-fixture swap | **Pass** (not click-verified) |
| P1-07 | Ads without consent | Loader gated; no `adsbygoogle.js` in HTML | **Pass** (stopgap; no CMP) |
| P1-08 | Dead ad rails | Collapse logic shipped; no live slot ids | **Pass** (not click-verified) |
| P1-09 | Production data proof | This report | **Done** |

---

## Failures found this loop

No new code defects in the deployed Phase 1 honesty path. F1/F2 from the previous loop are live.

### R1 — Phase 1 not deployed — **closed**

- **Severity:** was P0
- **Evidence:** health envelope, current-week `stale`, standings `{ rows }`, Vercel `46ba2b2` production READY.

### R2 — Live 2026/27 ingest still missing — **open**

- **Severity:** P0 (product) / blocked (provider)
- **Reproduction:** `GET /api/health` → `lastScoreSyncError` = “Score sync fetched 0 Premier League fixtures from the live sports provider.” `matchCount` stays **20**, ids stay `pl26-*`, upcoming is **[]**, current week stays **2**.
- **Root cause:** API-Football `fixtures?league=39&season=2026` is returning nothing the mapper accepts. Jobs fail visibly and do not substitute mock. Possible causes: empty season in the provider, wrong season year, rate limit, or mapping filter.
- **Files:** `ScoreSyncJob`, `ApiFootballProvider`
- **Recommended fix:** Inspect Render logs / API-Football dashboard for league 39 season 2026. If the provider has fixtures, next successful sync should add `apifb-*` rows and current week can move. If it has none, keep `stale`/`failed` — do not mock-fill. Do not auto-delete `pl26-*` (predictions may reference them).

### R3 — No interactive browser pass — **open**

- **Severity:** P2
- **Recommended fix:** Walk `/`, `/matchweek`, `/table` at 1440 and 375: stale copy, sample-fixture subtitle, no empty ad rails, open picks 0 with explanation.

---

## Acceptance criteria (Phase 1 slice)

From `16-ACCEPTANCE-CRITERIA.md`, only football-data / ads / engineering items that belong to Phase 1:

| Criterion | Production now |
|---|---|
| Current matchweek resolves correctly | **Fail** — mock MW2, labelled stale |
| Fixtures display when expected | **Partial** — MW2 cards exist; overdue; no open upcoming |
| Results settle predictions | Blocked (no new live FT beyond stored MW1) |
| Standings display or fail gracefully | **Pass** — envelope `stale`, table still shown |
| Data jobs observable | **Pass** via `/api/health` (admin UI 401) |
| Ad placeholders do not remain empty | **Pass** (script not loaded; rails collapse in code) |
| Consent respected | **Pass** (stopgap: no grant → no request) |
| Tests for changed paths | **Pass** locally |
| Implementation log updated | Yes |

Product/Studio/pundit/receipt criteria are Phase 3+. Not used to gate Phase 1. Studio remains in nav.

---

## Out of scope (do not treat as Phase 1 failures)

- Follow pundits, receipts, Studio packs, nav IA, World Cup docs archive, CMP UI, Aura dual-truth — later phases.
- Studio remains in nav and must stay.

---

## Next action

1. Diagnose API-Football empty fixture response for league 39 / season 2026 (Render logs + provider dashboard). That is the remaining Phase 1 P0.
2. Optional: click-verify `/`, `/matchweek`, `/table` (R3).
3. Do **not** start Phase 2 until either live `apifb-*` MW≥3 exists **or** you explicitly accept “stale mock calendar + failed score-sync” as the production-integrity end state.
4. Do not mock-fill production to make the homepage look busy.
