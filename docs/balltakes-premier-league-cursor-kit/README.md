# BallTakes Premier League Evolution - Cursor Kit

This package is the working instruction set for evolving the existing BallTakes World Cup application into a Premier League-first football prediction and AI banter platform.

## Product direction

BallTakes is moving from a one-off World Cup prediction experience into an ongoing club-football product built around:

1. User match predictions and season predictions.
2. Private leagues and leaderboards.
3. Tracking what pundits and social analysts predict, with source provenance.
4. Comparing user performance against pundits.
5. Generating shareable banter, memes, GIF concepts, images and later short videos from prediction outcomes and receipts.
6. Anonymous guest play with recovery keys, plus optional Supabase accounts.
7. Paid AI content generation using Stripe and a credit/entitlement model.
8. A more interactive, mobile-first UI supported by football data, RSS, YouTube and other permitted media sources.

## Scope decision

### Now
Premier League only.

### Next
Other domestic club leagues such as La Liga, Serie A, Bundesliga, Ligue 1 and similar leagues, based on user preference.

### Explicitly deferred to a separate future plan
- World Cup
- Euros
- AFCON
- Copa America
- International tournament structures
- Domestic/continental knockout cup structures
- Group stages and brackets

Do not over-engineer the current implementation for these deferred formats.

## How to use this package in Cursor

Start with `00_CURSOR_MASTER_PROMPT.md`. Cursor should then treat the rest of this folder as persistent project instructions and working references.

Recommended reading order:

1. `00_CURSOR_MASTER_PROMPT.md`
2. `01_PRODUCT_SCOPE.md`
3. `02_TARGET_ARCHITECTURE.md`
4. `03_PHASED_ROLLOUT.md`
5. `04_PUNDITS_AND_SCORING.md`
6. `05_AUTH_GUESTS_AND_USERS.md`
7. `06_AI_CONTENT_AND_MONETIZATION.md`
8. `07_UI_UX_AND_ENGAGEMENT.md`
9. `08_AGENT_EXECUTION_LOOP.md`
10. `09_DEFINITION_OF_DONE.md`

Cursor should create and maintain implementation-specific files under `docs/balltakes-evolution/` inside the repository as instructed in the master prompt.
