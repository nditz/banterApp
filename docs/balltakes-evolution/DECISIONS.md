# Decisions

## D1 — Wipe World Cup data

Kit said preserve historical WC data. Product owner instructed a clean break. Migration truncates WC fixtures, predictions, bracket picks, bonuses, standings, and related live rows. User accounts and private-league memberships are kept.

## D2 — Core product only

Kit phases 6–11 (pundits, receipts, Stripe, AI monetization) are deferred. Existing Studio/feed/pundit plumbing stays but is not expanded.

## D3 — Friend groups vs football competitions

`leagues` remain prediction groups. Football competitions use new `competitions` / `competition_seasons` / `matchweeks` tables.

## D4 — Season awards reuse bonus tables

Collapse `/bonuses` and WC `UserPrediction` country types into one awards API. Keep `tournament_bonus_picks` / `tournament_award_results` tables (renamed in the API to season awards) to avoid a second awards schema. Add `SlotIndex` for top-four and relegation multi-picks.

## D5 — Denormalized match display fields

Matches keep `TeamA` / `TeamB` / codes and gain logo URLs plus `MatchweekNumber` so list queries do not always join teams. Canonical FKs still point at season and matchweek.

## D6 — Perfect matchweek, not group stage

Delete unused perfect-group-stage bonus. Award +5 when every result pick in a finished matchweek is correct. Persist via `matchweek_bonuses` so leaderboards stay a simple sum.

## D7 — Mock provider ships PL fixtures

When `SportsData:Provider` is `mock`, serve a Premier League 2026/27-shaped fixture list so local/dev works without API keys.
