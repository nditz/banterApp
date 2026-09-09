# Verification Report

**Date:** 2026-09-09  
**Phase under test:** Phase 1 — Production Integrity  
**Method:** Completion loop (`cursor/COMPLETION-LOOP.md` + `cursor/VERIFY-PROMPT.md`). Runtime production probes against `api.balltakes.com` / `balltakes.com`, local tests/lint/typecheck/production build, and code inspection of football read paths, jobs, ads, and empty/error/stale states.  
**Browser:** No interactive browser tools in this session. Public routes were fetched as HTML; fixture/table bodies are client-rendered so visual stale banners were inferred from API + client logic, not clicked through.

Phase 1 is **not complete**. P0/P1 remain in production.

---

## Gate

| Question | Result |
|---|---|
| All current-phase P0/P1 passing in production? | **No** |
| Mark Phase 1 complete? | **No** |
| Start Phase 2? | **No** — new deliberate run only after Phase 1 is verified post-deploy |

---

## Local verification (this run)

| Check | Result |
|---|---|
| Backend tests | **307 passed** (0 failed) |
| Frontend Vitest | **21 passed** (0 failed) |
| ESLint | Clean |
| `tsc --noEmit` | Clean |
| Next.js production build | Succeeded (32 routes) |

Code compiles. That is not treated as a pass.

---

## Production runtime (2026-09-09, live site still on **pre–Phase 1** API)

### `GET https://api.balltakes.com/api/health`

```json
{
  "status": "ok",
  "database": { "connected": true, "provider": "postgresql", "matchCount": 20, "newsCount": 42 },
  "sportsData": { "provider": "apifootball", "mode": "apifootball-live", "syncIntervalMinutes": 15 }
}
```

Missing Phase 1 fields: `lastScoreSyncStatus`, `overdueUnfinishedFixtures`, `degraded` when fixtures are overdue. Health reports **ok** while the calendar is overdue mock data.

### `GET /api/matchweeks/current`

- Shape: `{ number, matches }` only — **no** `status` / `source` / `official` / `error`
- `number`: **2**
- IDs: `pl26-mw2-*` (mock)
- All ten rows `NS`, kickoffs 28–31 Aug 2026, `isLocked: true`

### `GET /api/standings`

Bare array of 20 clubs (not `{ status, rows }`). Brighton 1st on 1 game; Chelsea/Fulham `played: 0`. Looks official.

### `GET /api/matches/upcoming`

Returns **11 overdue `NS` mock rows** (`pl26-mw1-10` plus all MW2). Kickoffs are in the past. Homepage therefore shows **0 open picks** (`isMatchLocked`).

### Public HTML

- `/` — “Open picks: 0”. Matchweek / table / banter sections present. No receipts. Client fixture list not in SSR HTML.
- `/matchweek` — heading “Current matchweek” / Premier League 2026/27; cards client-rendered.
- `/table` — standings heading; rows client-rendered.

---

## Phase 1 backlog vs evidence

| ID | Item | Local code | Production | Status |
|---|---|---|---|---|
| P1-01 | Live sports provider | Live calls throw; no mock seed into live DB | Still `apifootball-live` serving 20 `pl26-*` rows | **P0 fail (prod)** |
| P1-02 | Current matchweek | Envelope + stale; resolver unchanged | MW2 mock, no stale flag | **P0 fail (prod)** |
| P1-03 | Mock calendar MW4 | Done in code | Prod DB still MW1–2 only | Done locally |
| P1-04 | Standings empty vs failed | Envelope + stale | Bare array, looks official | **P1 fail (prod)** |
| P1-05 | Hangfire silence | Rethrow + Fail + nextRun/error | Not deployed; health has no last-sync fields | **P1 unverified in prod** |
| P1-06 | Empty/error/stale UI | Distinct copy; client infers stale/`pl26-` unofficial | Live UI has no status field; treats mock as a real week | **P1 fail until frontend deploy** |
| P1-07 | Ads without consent | Loader/slots off until grant | Not interactively verified | Code done (stopgap) |
| P1-08 | Dead ad rails | Rails collapse without consent/slot | Not interactively verified | Code done |
| P1-09 | Production data proof | This report | 20 matches, MW2 overdue, standings MW1-shaped | **Done (proof of broken live set)** |

---

## Failures found this loop (fixed in repo)

### F1 — Read path live-substituted fixtures over a stored calendar

- **Severity:** P0 (Phase 1 silent-error / split-brain)
- **Reproduction:** Store only overdue PL `NS` rows. `GET /api/matches/upcoming` (and matchweek/results/by-id when the filtered query was empty) called the sports provider and could return a different week than `/api/matchweeks/current`.
- **Root cause:** Empty filtered query was treated as “no data” even when the DB already had a PL calendar.
- **Files:** `MatchEndpoints.cs`
- **Fix (this run):** If Premier League rows exist in the database, those endpoints return the stored (possibly empty) result. Provider is only used when the DB has zero PL matches, and provider failures map to empty/404 instead of 500.
- **Tests:** `MatchReadPathTests`

### F2 — Client treated legacy current-week payloads as official/`ok`

- **Severity:** P1
- **Reproduction:** Payload `{ number: 2, matches: [NS kickoff Aug 2026] }` with no `status` → UI said current matchweek, BBC-style copy.
- **Root cause:** `datasetStatusFromMatchweek` only trusted `payload.status`.
- **Files:** `football-dataset.ts`, `MatchweekBoard.tsx`, `PredictionCenter.tsx`
- **Fix (this run):** Infer `stale` from overdue unfinished kickoffs; infer unofficial from `pl26-*` ids when `official` is omitted. Removed `useMatch` 404 → `mockMatches` substitution.

---

## Remaining open failures (not fixed here)

### R1 — Phase 1 API not deployed

- **Severity:** P0
- **Reproduction:** Hit the production URLs above. Compare to local envelope (`status`, `rows`, health sync fields).
- **Root cause:** Render/Vercel still serving the previous API/frontend.
- **Recommended fix:** Deploy API + frontend from this branch. Re-run this report. Expect health `degraded`, current week `stale`, standings envelope `stale`.

### R2 — Live 2026/27 ingest still missing

- **Severity:** P0 (product) / blocked (external)
- **Reproduction:** After deploy, `matchCount` still 20 and ids still `pl26-*`.
- **Root cause:** API-Football `fixtures?league=39&season=2026` has not written `apifb-*` rows. Either the season calendar is empty, the key/rate limit fails, or jobs have not succeeded since the last mock seed.
- **Files:** `ScoreSyncJob`, `ApiFootballProvider`
- **Recommended fix:** After deploy, read admin jobs + `sync_runs`. If the provider returns fixtures, current week should move off mock MW2. If it returns 0, keep `stale`/`error` — do not mock-fill. Do not auto-delete `pl26-*` (predictions may reference them).

### R3 — No interactive browser pass

- **Severity:** P2 for this loop (does not override R1)
- **Recommended fix:** After deploy, walk `/`, `/matchweek`, `/table` on desktop and 375px: stale copy, no empty ad rails, no demo fixture swap.

---

## Acceptance criteria (Phase 1 slice)

From `16-ACCEPTANCE-CRITERIA.md`, only football-data / ads / engineering items that belong to Phase 1:

| Criterion | Local | Production |
|---|---|---|
| Current matchweek resolves correctly | Pass on mock MW3 (9 Sep 2026) | **Fail** — mock MW2 |
| Fixtures display when expected | Stale/error copy when overdue/failed | Mock MW2 shown as current |
| Results settle predictions | Out of this loop (needs live FT) | Blocked |
| Standings display or fail gracefully | Envelope + UI | Bare array, looks live |
| Data jobs observable in admin | Code present | Not verified (auth) |
| Ad placeholders do not remain empty | Collapse without consent/slot | Not interactively verified |
| Consent respected | No ads until `granted` | Stopgap in code |
| Tests for changed paths | Yes | n/a |
| Implementation log updated | Yes | n/a |

Product/Studio/pundit/receipt criteria are Phase 3+. Not used to gate Phase 1.

---

## Out of scope (do not treat as Phase 1 failures)

- Follow pundits, receipts, Studio packs, nav IA, World Cup docs archive, CMP UI, Aura dual-truth — later phases.
- Studio remains in nav and must stay.

---

## Next action

1. Deploy backend + frontend.
2. Re-probe `/api/health`, `/api/matchweeks/current`, `/api/standings`, admin jobs.
3. Confirm UI stale copy on `/` and `/matchweek` while `pl26-*` remain.
4. Only then either (a) mark Phase 1 complete if stale/error is correct and jobs are visible, or (b) keep blocked if live ingest is still silent.
5. Do not start Phase 2 in the same run.
