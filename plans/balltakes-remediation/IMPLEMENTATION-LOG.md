# Implementation Log

## Phase
Phase 4 — Receipt Engine  
(Also closed remaining Phase 3 P0/P1 found on the production re-scan.)

## Date
2026-09-09

## Existing Implementation Found
- Settlement (`PredictionRescoreService`) updated points only. No receipt rows.
- `/predictions/history` listed picks with `PredictionReactionCard`. `PredictionReceiptCard` existed only as an ephemeral share card after save.
- Personalized feed copy claimed “Receipts are public”. Public feed did not query a receipts table (none existed).
- Phase 3 follow/comparison was already in production (`#41`) but guest `GET /api/studio/comparison?matchIds=` 500ed, and `/pundits` SSR showed empty because `isLoading` is false while the query is idle.

## Changes Made
- **P3 follow-up:** Guest comparison loads pundit rows without calling EF `ToListAsync` on `Enumerable.Empty()`. `/pundits` treats `isPending` as loading and fetches the directory without waiting on session.
- **P4-01** `PredictionReceipt` + `ReceiptStoryCandidate` on existing `Prediction` / `Match`. Owner XOR. Unique `(PredictionId, ResultHash)`. RLS enabled.
- **P4-02** Score-sync rescore emits receipts even when points did not change. Same result version is a no-op; a new scoreline creates a new version.
- **P4-03** `/predictions/history` is Receipts. Reuses `PredictionReceiptCard` + `PredictionReactionCard`. Nav/Me overflow label Receipts (URL unchanged).
- **P4-04** Classifier: exact score, beat pundit, pundit beat user, majority wrong / minority right, hit/miss. Summaries are factual scorelines, not invented quotes.
- **P4-05** `IsPublic` always false. List/get require session and owner match. 404 for other owners. Public feed is not backed by receipts; personal miss copy no longer says receipts are public.

## Files Changed
- Backend: entities, AppDbContext, `ReceiptSettlementService`, `ReceiptQueryService`, `ReceiptEndpoints`, `PredictionRescoreService`, `StudioComparisonService`, `PersonalizedFeedService`, Program DI, migration `20260909204154_AddPredictionReceipts`.
- Frontend: `PunditsDirectory` / `usePundits`, `/predictions/history`, `useReceipts`, nav, Me hub, types.
- Tests: comparison guest, settlement/privacy/endpoints, nav Receipts, story labels.

## Database / Migration Changes
`20260909204154_AddPredictionReceipts` — `prediction_receipts` + `receipt_story_candidates`. **Must be applied on production Postgres before receipt APIs work.**

## API Changes
- `GET /api/receipts` — owner list (latest version per prediction). Session + terms required.
- `GET /api/receipts/{id}` — owner only; otherwise 404.
- Guest `GET /api/studio/comparison?matchIds=` no longer 500s.

DTOs omit user/anonymous ids. Aura delta stored as the existing points value (no second scoring system).

## Tests Added/Updated
- Backend: **379 passed** (was 367).
- Frontend Vitest: **37 passed** (was 35).
- Lint: eslint clean on touched files. Typecheck: `tsc --noEmit` clean.
- Production build: Next.js succeeded; `/predictions/history` in the route list.

## Risks / Follow-up
- Production needs this API + frontend deploy and the receipts migration. Until then `/api/receipts` is absent and `/pundits` can still flash empty.
- Receipts for matches that finished before deploy appear after the next score-sync (rescore emits even if points already match).
- Guest Terms overlay can still block More → Receipts (pre-existing).
- Do not put receipts on the public timeline. Do not invent pundit quotes. Do not remove Studio.

## Verification Results
See `plans/balltakes-remediation/VERIFICATION-REPORT.md`.

## Acceptance Criteria Status
- [x] Passed — receipt persistence, settlement idempotency, Receipts UI, story candidates, privacy (local)
- [ ] Partial — production migrate/deploy; Phase 3 empty-directory / comparison-500 still live until this deploy
- [ ] Blocked

**Phase complete:** locally yes. Start Phase 5 only after production verification of Phase 4.
