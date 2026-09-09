# Verification Report

**Date:** 2026-09-09 (re-run after `main` `#38` / `b8ed852`)  
**Phase under test:** Phase 1 — Production Integrity  
**Method:** Completion loop (`cursor/COMPLETION-LOOP.md` + `cursor/VERIFY-PROMPT.md`). Runtime probes against `api.balltakes.com` / `balltakes.com`, Vercel production deployment for `balltakes`, local tests, and inspection of football read paths, jobs, ads, and empty/error/stale states.  
**Browser:** No interactive click-through. Public HTML plus shipped JS chunks were inspected. Stale banners are client-rendered.

Phase 1 integrity is **still deployed**. Live ingest is **still empty**. Phase 1 is **not complete**.

A football-data.org mapper fix (`Group=PL`) exists on the local `feat/fix-rss-feeds-adsense` working tree. It is **not** on `main` / Render. Production therefore still has 20 `pl26-*` rows.

---

## Gate

| Question | Result |
|---|---|
| All current-phase P0/P1 passing in production? | **No** — R2 (live ingest) remains P0 |
| Mark Phase 1 complete? | **No** |
| Start Phase 2? | **No** |

Frontend production is `READY` on [balltakes `#38`](https://vercel.com/stanley-kamau-s-projects/balltakes/6fKfh4LeXxmb7diAZ1zvBHQG6F4M) (`dpl_6fKfh4LeXxmb7diAZ1zvBHQG6F4M`, sha `b8ed852`). API remains `degraded` with overdue mock MW2.

---

## Local verification (this run)

| Check | Result |
|---|---|
| Backend tests | **327 passed** (0 failed) — includes uncommitted working-tree tests (GIF library + football-data mapper) not yet on `main` |
| Frontend Vitest | **21 passed** (0 failed) |
| Next.js production build | **Vercel production READY** on `b8ed852` |

Code compiling / shipping is not treated as a product pass.

---

## Production runtime (2026-09-09, **post–`#38` / football-data token**)

### `GET https://api.balltakes.com/api/health`

```json
{
  "status": "degraded",
  "database": { "connected": true, "provider": "postgresql", "matchCount": 20, "newsCount": 42 },
  "sportsData": {
    "provider": "apifootball",
    "mode": "apifootball-live",
    "syncIntervalMinutes": 15,
    "lastScoreSyncStatus": "completed",
    "lastScoreSyncAt": "2026-09-09T14:45:04.882034+00:00",
    "lastScoreSyncError": null,
    "lastScoreSyncItems": 0,
    "overdueUnfinishedFixtures": true
  }
}
```

Health is still `degraded` because MW2 kickoffs are overdue without results. Score-sync no longer reports `failed`. The latest run **completed with 0 items** and a null error. That is weaker honesty than the previous `failed` + “fetched 0 Premier League fixtures” text (see R2 / R4).

Did **not** mock-fill: `matchCount` stays **20**.

### `GET /api/matchweeks/current`

- Envelope present: `status=stale`, `source=mock`, `official=false`
- `error`: “These fixtures are overdue without results. Score sync may be failing.”
- `number`: **2**
- 10 rows, all `pl26-mw2-*`, all `NS`, kickoffs 28–30 Aug 2026
- **0** `apifb-*` or `fd-*` ids

### `GET /api/standings`

Envelope `{ status, source, lastSyncedAt, error, rows }` (not a bare array).

- `status=stale`, `source=computed`, 20 rows
- `error`: standings based on finished matches while some kickoffs are overdue
- Brighton 1st (1 game); Chelsea/Fulham `played: 0`

### `GET /api/matches/upcoming`

**0 rows.** Stored PL calendar exists; upcoming filters to kickoffs after `now−3h`; remaining `NS` rows are in the past. Homepage open-picks count is **0** for a real reason.

### `GET /api/matches/results`

9 finished MW1 rows (`pl26-mw1-*`).

### Public site

- Vercel production: [balltakes `#38`](https://vercel.com/stanley-kamau-s-projects/balltakes/6fKfh4LeXxmb7diAZ1zvBHQG6F4M) `READY`, commit `b8ed852`.
- `/` — title Premier League; “Open picks” **0**; Studio present; **no** World Cup/FIFA copy; **no** `adsbygoogle.js` in HTML (`ca-pub-` only via `google-adsense-account` meta).
- `/matchweek` JS includes “sample fixtures” / “local/dev” and “overdue without results”.
- `/table` JS includes “overdue without results”.
- Ads consent helper `balltakes_advertising_consent` / `canRequestAds` is in shipped chunks. `adsbygoogle.js` is not injected without consent.
- `/studio` — Content Studio (not removed).
- Admin `/api/admin/jobs` and `/api/admin/health` return **401** without auth (expected). Public `/api/health` remains the unauthenticated job signal.

---

## Phase 1 backlog vs evidence

| ID | Item | Production | Status |
|---|---|---|---|
| P1-01 | Live sports provider | `apifootball-live`; latest score-sync **completed** with **0** items; did **not** mock-fill | **Pass (no mock-fill)** / **P0 remain: no live rows** |
| P1-02 | Current matchweek | Envelope `stale` + unofficial mock MW2 | **Pass (visible)** / **Fail (not a real open week)** |
| P1-03 | Mock calendar MW4 | Code on main; prod DB still MW1–2 only | Done in code; unused in prod until empty DB seed |
| P1-04 | Standings empty vs failed | Envelope `stale` + 20 computed rows | **Pass** |
| P1-05 | Hangfire silence | Health still `degraded` (overdue). Latest score-sync is `completed` / 0 items / null error — **less loud than before** | **Partial** |
| P1-06 | Empty/error/stale UI | Stale copy in production JS; no demo-fixture swap | **Pass** (not click-verified) |
| P1-07 | Ads without consent | Loader gated; no `adsbygoogle.js` in HTML | **Pass** (stopgap; no CMP) |
| P1-08 | Dead ad rails | Collapse logic shipped; no live slot ids | **Pass** (not click-verified) |
| P1-09 | Production data proof | This report | **Done** |

---

## Failures found this loop

### R2 — Live 2026/27 ingest still missing — **open**

- **Severity:** P0 (product)
- **Reproduction:** `GET /api/health` → `matchCount=20`, `lastScoreSyncItems=0`. `GET /api/matchweeks/current` stays mock MW2 `pl26-*`. Upcoming is `[]`.
- **Root cause:** API-Football still returns nothing usable for league 39 / season 2026. football-data.org is now configured as fallback and returns matches, but production mapping sets `Group=""` so `PremierLeagueMatchScope` drops every `fd-*` row. The job then **completes** with 0 upserts instead of failing.
- **Files:** `ScoreSyncJob`, `FootballDataProvider`, `FootballDataFixtureMapper` (local, not deployed), `PremierLeagueMatchScope`
- **Recommended fix:** Ship the local mapper (`Group=PL`, status `FINISHED→FT` / `TIMED→NS`) and redeploy Render. After a successful sync, expect `fd-*` PL rows and current week to move. Do not mock-fill. Do not auto-delete `pl26-*` (predictions may reference them).

### R4 — Score-sync honesty regression — **open** (new this run)

- **Severity:** P1 (observability) — does not replace R2
- **Reproduction:** Compare previous health (`lastScoreSyncStatus=failed`, error “fetched 0 Premier League fixtures”) with this run (`completed`, `lastScoreSyncError=null`, `items=0`). Overall `/api/health` is still `degraded` via `overdueUnfinishedFixtures`.
- **Root cause:** Fallback returns a non-empty fixture list; PL filter then yields zero rows; `CompleteAsync` still runs.
- **Recommended fix:** Treat “fallback returned rows but 0 survived PL scope” as a failed/warning sync (or ship the mapper so rows survive). Keep `overdueUnfinishedFixtures`.

### R3 — No interactive browser pass — **open**

- **Severity:** P2
- **Recommended fix:** Walk `/`, `/matchweek`, `/table` at 1440 and 375: stale copy, sample-fixture subtitle, no empty ad rails, open picks 0 with explanation.

### R1 — Phase 1 not deployed — **closed**

Still closed. `#37` / `#38` are on `main`; Vercel production is `READY`.

---

## Acceptance criteria (Phase 1 slice)

From `16-ACCEPTANCE-CRITERIA.md`, only football-data / ads / engineering items that belong to Phase 1:

| Criterion | Production now |
|---|---|
| Current matchweek resolves correctly | **Fail** — mock MW2, labelled stale |
| Fixtures display when expected | **Partial** — MW2 cards exist; overdue; no open upcoming |
| Results settle predictions | Blocked (no new live FT beyond stored MW1) |
| Standings display or fail gracefully | **Pass** — envelope `stale`, table still shown |
| Data jobs observable | **Partial** — health `degraded`; latest score-sync looks `completed` with 0 items |
| Ad placeholders do not remain empty | **Pass** (script not loaded; rails collapse in code) |
| Consent respected | **Pass** (stopgap: no grant → no request) |
| Tests for changed paths | **Pass** locally |
| Implementation log updated | Yes |

Product/Studio/pundit/receipt criteria are Phase 3+. Not used to gate Phase 1. Studio remains in nav.

---

## Out of scope (do not treat as Phase 1 failures)

- Follow pundits, receipts, Studio packs, nav IA, World Cup docs archive, CMP UI, Aura dual-truth — later phases.
- GIF library / Giphy throttle work on the local branch — Phase 6 adjacent; not a Phase 1 gate.
- Studio remains in nav and must stay.

---

## Next action

1. **Do not close Phase 1.** Live calendar is still mock MW2.
2. Deploy the football-data mapper (`Group=PL`) to Render, then re-run this report. Pass condition: `fd-*` (or `apifb-*`) Premier League rows, current week not unofficial MW2, upcoming no longer empty **or** honestly failed ingest with a non-null `lastScoreSyncError`.
3. Optional: click-verify `/`, `/matchweek`, `/table` (R3).
4. Do **not** start Phase 2 until either live `fd-*` / `apifb-*` MW≥3 exists **or** you explicitly accept “stale mock calendar + completed-0 score-sync” as the production-integrity end state.
5. Do not mock-fill production to make the homepage look busy.
