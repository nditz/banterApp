# Implementation Log

## Phase
Phase 2 — Product Cleanup

## Date
2026-09-09

## Existing Implementation Found
- Desktop `AppShell` listed Home, Matchweek, Table, Awards, Leagues, Studio, Rules, History. Mobile bottom was Home, Picks, Table, Leagues, Studio.
- `/awards` already hosted `TournamentBonusBoard`; copy still said Season awards.
- Homepage welcome was an 8-slide autoplay tour (`HOME_WELCOME_SLIDES` spread all `CONCEPT_SLIDES`).
- Studio already in desktop + mobile nav; sitemap priority was 0.5. vs-pundits tab said “fictional pundit desk personas”.
- `CountrySelector` / `PlayerSelector` / `useUserPredictions` had no route. Season calls already use `TournamentBonus*` APIs.
- WC docs and `supabase/seed.sql` still described World Cup 2026 group fixtures. `/brackets` redirect and `WorldCupLegacyPurge` already exist.

## Changes Made
- Shared nav config: Predict / Banter / Studio / Leagues as the spine. Desktop adds Season Calls + More (Table, History, Rules). Mobile bottom is Predict / Banter / Studio (center) / Leagues / Me. Table is overflow-only.
- Added `/me` hub (history, season calls, table, Studio, Aura rankings). Account control remains the signed-in profile.
- Reframed Awards as Season Calls in nav, page title, rules, leagues, and bonus board. URL stays `/awards`. No API rename.
- Homepage welcome is three slides (picks, feed, Studio) with Lock a pick / Watch the feed / Open Studio. Full concept tour stays on `/rules`.
- Archived World Cup docs to `docs/_archive/world-cup/`. Seed SQL is a do-not-use notice. Live/mock providers still own fixtures.
- Removed orphan prediction selectors and the unused UserPrediction client hook. Backend UserPrediction endpoints kept.
- Studio sitemap priority 0.9. vs-pundits copy now refers to sourced pundit predictions.

## Files Changed
- Frontend: `navigation.ts`, `AppShell`, `MobileBottomNav`, `HomeQuickNav`, `HomeWelcomePanel`, `MeHub`, `/me`, `sitemap-routes.ts`, `StudioPage`, awards/rules/leagues copy, `TournamentBonusBoard`, `scoring-rules.ts`.
- Removed: `CountrySelector`, `PlayerSelector`, `useUserPredictions`, `useFootballReference`.
- Docs/seed: `docs/_archive/world-cup/*`, `supabase/seed.sql`, `supabase/README.md`, `docs/MEDIA-FEED-INTEGRATION.md`, `docs/DATA-SOURCES-INTERNAL.md`.
- Tests: `navigation.test.ts`, `sitemap-routes.test.ts`, `scoring-rules.test.ts`.

## Database / Migration Changes
None. `TournamentBonus*` tables/routes unchanged.

## API Changes
None.

## Tests Added/Updated
- Frontend Vitest: **31 passed** (nav IA, welcome length, sitemap Studio priority).
- Lint: eslint clean (`--max-warnings 0`). Typecheck: `tsc --noEmit` clean.
- Production build: Next.js 16 succeeded; `/me` and `/studio` in the route list.
- Local `next start` SSR: home has Start here / Lock a pick / Watch the feed / Season Calls; awards h1 is Season Calls; `/brackets` still 308; sitemap `/studio` priority 0.9.
- Backend tests not re-run (no API/code changes).

## Risks / Follow-up
- `/awards` URL is unchanged; bookmarks still work. A later rename migration is optional.
- Backend `/api/user/predictions` is unused by the UI. Leave until a dedicated cleanup; do not confuse with match `Prediction` or tournament bonuses.
- Interactive 375/1440 click-through of the new bottom nav was not done in a real browser (no browser tools this run). SSR + unit tests cover labels and routes.
- Do not restore archived WC docs into live product paths. Do not run old WC INSERT statements in production SQL Editor.

## Verification Results
- Local production server: `/`, `/awards`, `/studio`, `/me`, `/matchweek`, `/table`, `/sitemap.xml` returned 200.
- Home no longer includes the long “Quick tour” / “Beat the board” slides.
- Studio HTML no longer contains “fictional pundit” / “pundit desk”.
- `/brackets` remains a redirect.

## Acceptance Criteria Status
- [x] Passed — Phase 2 IA/copy/cleanup (nav coherent; Studio central; no stale WC UI in PL flows; `/awards` reframed; tests for changed paths; log updated)
- [ ] Partial — visual click-through of mobile bottom nav / More menu not done in a browser
- [ ] Blocked

**Phase complete:** yes for Phase 2 product cleanup. Start Phase 3 (Pundits & Comparison) only in a new deliberate task.
