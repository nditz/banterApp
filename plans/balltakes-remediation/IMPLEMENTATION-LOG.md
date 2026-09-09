# Implementation Log

## Phase
Phase 1 — Production Integrity

## Date
2026-09-09

## Existing Implementation Found
- Sports pipeline already exists: `ScoreSyncJob`, `StandingsSyncJob`, `CurrentMatchweek.Resolve`, `ApiFootballProvider`, `MockSportsDataProvider`.
- Production `/api/health` reports `SportsData.provider=apifootball` and `mode=apifootball-live`.
- Production still only has **20** `pl26-*` mock fixtures (MW1–2). Current matchweek is **2**, all MW2 rows `NS` with kickoffs 28–31 Aug 2026 (overdue). Standings exist (20 rows) but reflect MW1 only (Chelsea/Fulham played 0).
- Root cause: live API-Football calls falling back to mock fixtures, then Hangfire completing without rethrowing.
- `AdSlot` / `PageWithSideAds` / `AdSenseLoader` already exist. No advertising CMP. Terms consent is not ad consent.
- Admin jobs UI already existed; `NextRunAt` was always null and exhausted Hangfire retries were deleted.

## Changes Made
- Stopped silent mock substitution when API-Football has a key; live fixture/standings failures throw `SportsDataUnavailableException`.
- Score/standings jobs: `FailAsync` then **rethrow**; `AttemptsExceededAction.Fail` instead of `Delete`; do not seed mock into a live-provider database.
- Extended mock calendar through MW4 (Sep 2026) for local/dev, marked `official=false`.
- Current matchweek and standings APIs now return `status` (`ok` / `empty` / `error` / `stale`) instead of looking official when data is overdue or mock.
- Frontend no longer swaps in demo fixtures on API failure; empty vs error vs stale copy is distinct.
- AdSense loader/slots do not run without explicit advertising consent (defaults unset). Side rails collapse when slots or consent are missing.
- Admin jobs populate `nextRunAt` from Hangfire and `lastErrorMessage` from `sync_runs`.
- Production startup requires `SportsData:ApiKey` when provider is `apifootball`. Health/launch checklist expose mock vs live and overdue fixtures.
- Completion-loop (2026-09-09): match list/read endpoints no longer live-substitute fixtures when a Premier League calendar is already stored. Client infers `stale` and unofficial `pl26-*` even if the API omits `status`/`official`.

## Files Changed
- Backend: `MockSportsDataProvider`, `ApiFootballProvider`, `ScoreSyncJob`, `StandingsSyncJob`, `MatchEndpoints`, `MatchDtos`, `JobRegistryService`, `ProductionStartupValidator`, `HealthEndpoints`, `AdminHealthService`, plus `SportsDataUnavailableException` and `FootballDatasetStatus`.
- Frontend: `useMatches`, `MatchweekBoard`, `PredictionCenter`, `LeagueTable`, `AdSlot`, `AdSenseLoader`, `PageWithSideAds`, `ads.ts`, `advertising-consent.ts`, `football-dataset.ts`, admin jobs page.
- Tests: matchweek/standings/startup/dataset tests; `MatchReadPathTests`; frontend dataset/consent tests.
- Docs: this log; `IMPLEMENTATION-BACKLOG.md` Phase 1 statuses.

## Database / Migration Changes
None.

## API Changes
- `GET /api/matchweeks/current` adds `status`, `source`, `official`, `error` (keeps `number` + `matches`).
- `GET /api/standings` is now `{ status, source, lastSyncedAt, error, rows }` instead of a bare array.
- `GET /api/health` adds score-sync fields and `overdueUnfinishedFixtures`; status may be `degraded`.
- Admin jobs DTO adds `lastErrorMessage`; `nextRunAt` is populated when Hangfire has a schedule.
- Admin health/launch checklist add live-provider and overdue-fixture checks.

## Tests Added/Updated
- Backend: 307 passed (`PremierLeagueMatchweekTests`, `MatchApiTests`, `MatchReadPathTests`, `FootballDatasetStatusTests`, `ProductionStartupValidatorTests`).
- Frontend Vitest: 21 passed.
- Lint: eslint clean. Typecheck: `tsc --noEmit` clean. Production build: Next.js 16 succeeded.

## Risks / Follow-up
- **Blocked on live fixture ingest:** production DB still holds 20 mock rows until a successful API-Football `fixtures?league=39&season=2026` response arrives. After this deploy, failed syncs should show in admin instead of rewriting mock data. If API-Football has no 2026/27 calendar yet, current week stays stale by design.
- Do not delete `pl26-*` rows automatically — predictions may reference them. Optional later purge after live `apifb-*` rows exist.
- Advertising CMP UI is Phase 8; Phase 1 stopgap is “no consent → no ads”.
- Standings clients must read `rows` (frontend updated). Any external client expecting a bare array will break.

## Verification Results
- Completion-loop report: `plans/balltakes-remediation/VERIFICATION-REPORT.md`.
- Production re-run (2026-09-09, **after `main` `#38` / `b8ed852`**):
  - `GET https://api.balltakes.com/api/health` → `status=degraded`, `matchCount=20`, `lastScoreSyncStatus=completed`, `lastScoreSyncItems=0`, `lastScoreSyncError=null`, `overdueUnfinishedFixtures=true`.
  - `GET /api/matchweeks/current` → number **2**, envelope `status=stale`, `source=mock`, `official=false`, 10× `pl26-mw2-*` all `NS`. No `fd-*` / `apifb-*`.
  - `GET /api/standings` → envelope `{ status, rows }`, `status=stale`, 20 clubs, Chelsea/Fulham played 0.
  - `GET /api/matches/upcoming` → **[]** (stored PL calendar exists; overdue NS rows are not upcoming).
  - Public `/` HTML: Open picks **0**; Studio present; no World Cup copy; no `adsbygoogle.js`.
  - Vercel production READY on `b8ed852` (`dpl_6fKfh4LeXxmb7diAZ1zvBHQG6F4M`).
- Local: backend 327 passed (working tree); frontend Vitest 21 passed. Football-data mapper (`Group=PL`) is local only — not on Render.
- Interactive browser walkthrough of stale banners / ad rails was not done (R3).

## Acceptance Criteria Status
- [ ] Passed
- [x] Partial — honesty path deployed (stale visible, no mock-fill); live current matchweek still blocked; score-sync now `completed` with 0 items (R4)
- [x] Blocked — R2 live ingest (0 PL fixtures persisted). Previous R1 (not deployed) is closed.

**Phase complete:** no. Do not start Phase 2 until live `fd-*` / `apifb-*` MW≥3 exists **or** stale mock + empty score-sync is explicitly accepted as the integrity end state.

Football data (this phase):
- Current matchweek resolver reused (not rewritten).
- Fixtures display or fail/stale visibly after deploy.
- Standings display or fail/stale visibly.
- Jobs observable; failures no longer deleted from Hangfire.
- Ad placeholders collapse without consent/slot ids.
- Consent respected for ads (no request before grant).
