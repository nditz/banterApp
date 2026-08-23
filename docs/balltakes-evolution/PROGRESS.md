# Progress

## Current phase

Premier League core product (refocus redo).

## Current objective

Harden the Premier League product: leftover WC sweep, tests, and builds.

## Completed

- Evolution docs (`CURRENT_STATE`, `MASTER_PLAN`, `DECISIONS`, `PROGRESS`).
- World Cup product surface removed (brackets, Annex C, WC routes, WC copy). Destructive migration wipes WC rows.
- Competition / Season / Matchweek domain; API-Football league 39 / season 2026.
- Matchweek pick flow, kickoff lock, rescore on full time, perfect-matchweek bonus.
- Private leagues season-scoped; season awards board with multi-slot top four / relegation.
- PL homepage, nav, table, rules, legal/SEO, feed queries.
- Admin health reports Premier League league id, season, and fixture counts.

## In progress

- None.

## Blocked

- None.

## Next executable task

Apply EF migration `20260822113624_PremierLeagueRefocus` against the live database when deploying.
