# Current State — BallTakes at Premier League pivot

Captured before the Premier League refocus implementation.

## Product

BallTakes is a Next.js + ASP.NET Core + Supabase prediction app branded **Ball Takes**. Until this pivot it was a FIFA World Cup 2026 tournament game: match picks, 12-group knockout brackets (FIFA Annex C), tournament bonuses, and country/award predictions.

## Stack

- Frontend: Next.js App Router under `frontend/src/`
- Backend: ASP.NET Core 9 Minimal APIs in `backend/BanterApp.Api/`
- Database: Supabase Postgres, schema owned by EF Core migrations
- Auth: Supabase Auth (email + Google) plus anonymous cookies and recovery tokens
- Jobs: Hangfire (fixtures, standings, news, reference data, pundit extraction)

## What already works (preserve)

- Match predictions: result +3, exact score +7, double chance +2
- Private / global / country prediction leagues and invite codes
- Anonymous play, recovery keys, guest-to-account session
- Studio, banter feed, admin console
- Sports data provider abstraction (`ISportsDataProvider`) with API-Football primary path
- Pundit tables and extraction jobs (not expanded in this redo)

## World Cup assumptions (remove)

- `Match` uses `Stage` + `Group` with no competition/season/matchweek
- Sync config hardcodes API-Football World Cup league id `1`, season `2026`
- Bracket engine, Annex C JSON, `/brackets`, knockout nav
- Duplicate award surfaces: `/bonuses` (`TournamentBonusPick`) and `/predictions*` (`UserPrediction` country types)
- Copy, legal, SEO, mock data, and news queries framed as World Cup / FIFA

## Known defects to fix in this redo

- `ScoreSyncJob` updates match scores but does not rescore `Predictions` after full time
- Perfect matchday bonus exists in `ScoringService` but is never awarded
- Perfect group-stage bonus is World Cup-only and unused

## Scope of this redo

Core Premier League product only. Pundit leaderboards, receipts, Stripe, and multi-league UX are deferred.
