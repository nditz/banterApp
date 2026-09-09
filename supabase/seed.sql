-- DO NOT USE this file to seed production fixtures.
--
-- Production Premier League calendar comes from the live football-data
-- sync (Hangfire ScoreSyncJob on the API). Local/dev fixtures come from
-- MockSportsDataProvider in BanterApp.Api — not from this SQL.
--
-- World Cup 2026 group fixtures were removed in Phase 2 (product cleanup).
-- WorldCupLegacyPurge deletes non-Premier-League match rows on API boot,
-- so a WC seed would not survive anyway.
--
-- Safe local setup:
--   1. Apply EF / Supabase migrations.
--   2. Run the API with SportsData:Provider=mock (or live with an API key).
--   3. Do not paste historical WC INSERT statements into production SQL Editor.

SELECT 'banterapp seed.sql is intentionally empty — do not load World Cup fixtures' AS notice;
