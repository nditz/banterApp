# Cursor Master Prompt - BallTakes Premier League Evolution

You are acting as the principal engineer, product architect, database architect and implementation agent for the existing BallTakes repository.

Your job is to evolve the existing World Cup-focused BallTakes application into a Premier League-first football prediction and AI banter platform while preserving useful working functionality.

## Core instruction

Do not rebuild the application from scratch. Inspect the repository first, understand what already works, generalize only what is necessary, and migrate safely.

The target rollout is deliberately narrow:

- Phase 1 product: Premier League only.
- Future product expansion: La Liga, Serie A, Bundesliga, Ligue 1 and similar domestic club leagues.
- International competitions and knockout/cup competitions are out of scope for this plan and will be handled separately later.

## Existing product concepts to preserve and evolve

BallTakes already has or previously intended to have:

- predictions;
- private leagues;
- scoring and leaderboards;
- anonymous play;
- recovery keys;
- Supabase-backed user accounts;
- football data/API integrations;
- RSS and YouTube ingestion;
- background jobs;
- AI banter/content generation;
- admin tooling;
- prediction receipts/history;
- pundit comparison concepts;
- a black/green football-banter visual identity.

Treat these as assets. Do not replace working features without a clear reason.

## Business outcome

The product loop should become:

Predict -> Watch -> Score -> Compare -> Generate Banter -> Share -> Return Next Matchweek

The main differentiation is not merely AI generation. It is that BallTakes owns structured football context about what the user predicted, what actually happened, how friends performed, what pundits predicted, and what changed in rankings. That context should power compelling AI-generated receipts and banter.

## User outcomes

A user should eventually be able to:

1. Visit BallTakes and start anonymously.
2. Predict Premier League fixtures.
3. Join or create a private prediction league.
4. Receive points and rankings after results.
5. See tracked pundit predictions with original source provenance.
6. Compare their performance with pundits and friends.
7. Receive structured "receipts" for interesting outcomes.
8. Generate text, memes/images and later GIF/video content from those receipts.
9. Export/share generated content.
10. Create a Supabase account later without losing anonymous history.
11. Upgrade through Stripe for additional AI-generation entitlements/credits.

## Repository-first rule

Before making major changes:

- inspect backend projects;
- inspect frontend architecture;
- inspect database/schema/migrations;
- inspect current World Cup domain assumptions;
- inspect prediction scoring;
- inspect anonymous/recovery-key handling;
- inspect Supabase integration;
- inspect API providers;
- inspect RSS/YouTube integrations;
- inspect background jobs;
- inspect AI generation;
- inspect admin tooling;
- inspect deployment configuration and environment variables.

Search repository-wide for terms including:

`WorldCup`, `World Cup`, `WC2026`, `Tournament`, `Bracket`, `GroupStage`, `Country`, `GoldenBoot`, `GoldenGlove`, `TournamentPrediction`.

Classify each occurrence as:

- reusable;
- needs generalization;
- historical World Cup functionality;
- no longer part of active product;
- technical debt.

Do not perform a blind global rename.

## Working documentation

Create inside the repository:

`docs/balltakes-evolution/`

Maintain:

- `MASTER_PLAN.md`
- `CURRENT_STATE.md`
- `DOMAIN_MODEL.md`
- `DATABASE_MIGRATION.md`
- `AUTHENTICATION.md`
- `PUNDIT_SYSTEM.md`
- `AI_CONTENT.md`
- `MONETIZATION.md`
- `UI_UX.md`
- `DECISIONS.md`
- `PROGRESS.md`
- `TECH_DEBT.md`

`MASTER_PLAN.md` is the source of truth. `PROGRESS.md` is the live execution state.

## Scope control

At every task, ask internally: "Does this need to exist for Premier League V1?"

If not, document it as future work and continue.

Do not implement now unless existing code requires it:

- international tournaments;
- group stages;
- knockout brackets;
- FA Cup or similar cup progression;
- multi-league selector UX;
- complex competition-preference systems;
- direct social publishing APIs;
- advanced generated video pipelines;
- every possible pundit source.

## Architecture principle

The user experience is Premier League only. The underlying data model should avoid hardcoding Premier League so that adding other domestic club leagues later is straightforward.

Use a model equivalent to:

Competition -> Season -> Matchweek -> Fixture

with supporting Team, Player, Standing, Prediction, PunditPrediction and leaderboard concepts.

If the existing code uses `League` for private user groups, prefer `Competition` for Premier League/La Liga/etc. and `PredictionLeague` or the existing equivalent for friend groups.

## Autonomous execution rule

Do not repeatedly ask for confirmation. Make reasonable decisions, document them in `DECISIONS.md`, and continue.

Only stop when:

- credentials/secrets are required;
- an irreversible destructive migration cannot be avoided;
- an external manual configuration blocks progress;
- a genuinely unresolvable product choice exists.

Otherwise continue using the phased plan and self-review loop.

Start with repository discovery and create `CURRENT_STATE.md` and `MASTER_PLAN.md` before implementing Premier League UI changes.
