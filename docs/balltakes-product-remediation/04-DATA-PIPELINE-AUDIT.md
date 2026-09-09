# Data Pipeline Audit

## Objective
Make football data failures visible, testable and recoverable.

## Required Pipeline Map
Cursor must document the actual current implementation for:

Football provider(s)
→ ingestion jobs
→ normalization
→ Supabase/storage
→ current competition
→ current matchweek
→ fixtures
→ results
→ standings
→ player/team stats
→ prediction settlement
→ Aura
→ receipt generation
→ Banter/Studio context

## Required Dataset Health
At minimum track:
- competitions;
- seasons;
- teams;
- players;
- fixtures;
- current matchweek;
- results;
- standings;
- top scorers;
- top assists;
- prediction settlement status;
- pundit content;
- receipts;
- generated banter.

## Health Metadata
Each syncable dataset/job should expose:
- last started;
- last completed;
- duration;
- records fetched;
- records inserted;
- records updated;
- records failed;
- status;
- last error;
- next scheduled run.

## Failure Rules
Never silently render an empty dataset if a backend call failed.

Frontend states must distinguish:
- no data exists;
- loading;
- provider unavailable;
- stale data;
- job failed;
- user has no items yet.

## Tests
Add tests for:
- current matchweek resolution;
- fixture mapping;
- duplicate fixture handling;
- timezone correctness;
- result finalization;
- standings normalization;
- idempotent background jobs;
- retries;
- partial provider response handling.

