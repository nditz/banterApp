# Implementation Log

## Phase
Phase 3 — Pundits & Comparison

## Date
2026-09-09

## Existing Implementation Found
- `Pundit` / `PunditPrediction` / `PunditOpinion` already exist. Studio comparison loaded **all** `PunditKind.Source` picks for the user's predicted matches via `StudioEndpoints`.
- `GET /api/pundits` listed Source pundits with opinion counts only. No follow graph. No browse page.
- `PunditDisplayResolver` already handled licensed vs parody attribution. Extract jobs already wrote match-linked `PunditPrediction` when resolution + match-level type succeeded. Admin review approved opinions without backfilling a prediction row.
- Feed pundit mix ignored follows. Matchweek cards had no user-vs-pundit strip.

## Changes Made
- **P3-01** `PunditFollow` on existing `Pundit` (user or anonymous, unique per owner+pundit). Source desks only.
- **P3-02** Browse/follow at `/pundits`. Overflow + Me hub links. Follows filter Studio comparison and the pundit feed when the session has follows; otherwise all reviewed Source takes still show (discovery).
- **P3-03** `StudioComparisonService` reused by Studio and `GET /api/studio/comparison?matchIds=`. Matchweek and homepage pick cards show before/after you-vs-pundits (hit/miss after FT).
- **P3-04** Directory and comparison still go through `PunditDisplayResolver`. Source URL stays on the pick. No fabricated quotes.
- **P3-05** Unreviewed/rejected opinions do not appear in comparison. Admin approve upserts a match-linked `PunditPrediction`. Admin health counts match-linked opinions vs prediction rows. Review queue unchanged.

## Files Changed
- Backend: `PunditFollow` entity, AppDbContext, `PunditFollowService`, `StudioComparisonService`, `PunditMatchPredictionSync`, Opinion/Studio/Feed/Admin review/health endpoints, Program DI.
- Frontend: `/pundits`, `PunditsDirectory`, `MatchPunditComparison`, MatchCard/MatchweekBoard/PredictionCenter, Studio follow CTA, nav overflow, Me hub, sitemap, admin health counts.
- Tests: follow + comparison unit tests; admin approve backfill; nav/sitemap/comparison-phase Vitest.

## Database / Migration Changes
`20260909195409_AddPunditFollows` — table `pundit_follows` with XOR owner check, unique filtered indexes, RLS enabled. **Must be applied on production Postgres before follow APIs work.**

## API Changes
- `GET /api/pundits` — extra fields: `predictionCount`, `isFollowed`, attribution.
- `GET /api/pundits/follows`
- `POST /api/pundits/{id}/follow` — session required (terms), Source only, max 40.
- `DELETE /api/pundits/{id}/follow`
- `GET /api/studio/comparison?matchIds=` — optional CSV; includes matches without a user pick; `followedPunditCount`, `filteringToFollows`, `wasCorrect` on picks.

No Turnstile on follow. CSRF still required on writes. `TournamentBonus*` and Studio itself unchanged in role.

## Tests Added/Updated
- Backend: **367 passed** (includes new follow/comparison/approve-link tests).
- Frontend Vitest: **35 passed**.
- Lint: eslint clean on touched files. Typecheck: `tsc --noEmit` clean.
- Production build: Next.js succeeded; `/pundits` in the route list.
- Local `next start` SSR: `/pundits` 200 (Compare your takes / Follow sourced); `/me` has Follow desks; `/studio` Content Studio; `/matchweek` 200 (comparison strip is client-fetched).

## Risks / Follow-up
- Production needs the new migration + API deploy. Until then follow POSTs 404/500.
- Comparison is empty when ingest has not linked reviewed `PunditPrediction`s to current MW fixtures — that is a data/job issue, not hidden as an empty week.
- `useStudio` still swallows API errors into an empty comparison (pre-existing Studio pattern).
- Guest Terms overlay can still block More → Pundits (pre-existing).
- Do not invent a second pundit table. Do not follow parody personas. Do not replace sourced quotes with generated copy.

## Verification Results
- Local only this run. See `plans/balltakes-remediation/VERIFICATION-REPORT.md`.
- Not in production yet. Do not mark the Phase 3 production gate until migrate + deploy + live follow/compare.

## Acceptance Criteria Status
- [x] Passed — follow model/UX, matchweek+Studio comparison, attribution, ingest backfill (local)
- [ ] Partial — production migrate/deploy; comparison empty if no match-linked Source picks for current MW
- [ ] Blocked

**Phase complete:** locally yes. Start Phase 4 only after production verification of Phase 3.
