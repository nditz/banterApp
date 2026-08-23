# Phased Rollout Plan

## Phase 0 - Repository audit

Goal: understand the current implementation before changing architecture.

Actions:

- map backend/frontend projects;
- inspect World Cup-specific models;
- inspect database and migrations;
- inspect prediction/scoring flow;
- inspect guest and recovery-key flow;
- inspect Supabase integration;
- inspect football providers;
- inspect RSS/YouTube integrations;
- inspect background jobs;
- inspect content studio/AI generation;
- inspect admin tooling;
- inspect deployment and secrets.

Output: repository-specific `CURRENT_STATE.md`, `MASTER_PLAN.md`, `DATABASE_MIGRATION.md`.

## Phase 1 - Remove active World Cup assumptions

Goal: make the core application capable of representing a domestic league season.

Implement/adapt:

- Competition
- CompetitionSeason
- Matchweek
- Fixture
- season team participation
- provider mappings

Preserve historical World Cup data/functionality where reasonable.

Do not implement tournament brackets/groups in the new core.

## Phase 2 - Premier League data

Goal: Premier League becomes the only active competition.

Sync and display:

- teams;
- players as needed;
- fixtures;
- results;
- standings;
- player statistics as needed.

Acceptance: current Premier League information works end-to-end without World Cup assumptions.

## Phase 3 - Premier League predictions

Implement/adapt:

- matchweek prediction flow;
- fixture locking;
- scoring;
- prediction history;
- matchweek scores;
- season scores;
- global Premier League leaderboard;
- season predictions.

Acceptance: an anonymous user can predict a full matchweek and receive correct points after results.

## Phase 4 - Private prediction leagues

Adapt the existing league system for a full domestic season.

Add/verify:

- season leaderboard;
- matchweek leaderboard;
- invite/join flow;
- ranking movement;
- prediction comparison.

Acceptance: friends can compete for the whole Premier League season.

## Phase 5 - Authentication and identity

Complete:

- Supabase email/password;
- Google OAuth;
- user profile;
- anonymous recovery;
- guest-to-account claim.

Acceptance: a guest can play for weeks, register later, and lose nothing.

## Phase 6 - Pundit foundation

Start with a curated set of pundits/analysts.

Implement:

- Pundit;
- PunditSource;
- PunditPrediction;
- source provenance;
- manual/admin verification;
- pundit scoring.

Acceptance: BallTakes can reliably state what selected pundits predicted and show the evidence source.

## Phase 7 - Pundit automation

Add where technically/permissibly available:

- RSS ingestion;
- YouTube ingestion;
- transcript/text extraction;
- AI prediction extraction;
- fixture matching;
- confidence scoring;
- admin review queue.

Acceptance: much of the pundit pipeline can operate automatically while low-confidence items remain reviewable.

## Phase 8 - Me vs Pundits

Implement:

- pundit leaderboard;
- user vs pundit summary;
- matchweek comparison;
- season comparison;
- head-to-head views;
- pundit receipts.

Acceptance: users can immediately answer, "How am I doing compared with the pundits?"

## Phase 9 - Receipts

Generate structured events for interesting outcomes such as:

- exact score;
- big miss;
- winning/losing streak;
- matchweek win;
- private-league movement;
- beat/lost to pundit.

Acceptance: meaningful prediction events become reusable structured context.

## Phase 10 - AI Content Studio

Evolve the existing studio around structured receipt context.

V1 formats:

- banter text;
- caption;
- roast;
- flex;
- meme copy;
- AI meme/image.

Acceptance: a user can turn a real prediction result into shareable content in a few taps.

## Phase 11 - Monetization

Implement:

- configurable plans;
- AI credits/entitlements;
- Stripe checkout;
- subscriptions;
- billing portal;
- idempotent webhooks;
- usage ledger.

Acceptance: paid entitlements are controlled server-side and AI usage is metered safely.

## Phase 12 - Feed and engagement

Generalize existing content ingestion into a Premier League-focused home experience:

- upcoming matches;
- user predictions;
- pundit takes;
- receipts;
- football news;
- YouTube/media;
- league table;
- private league movement;
- AI banter.

## Phase 13 - UI/UX redesign

Polish the complete end-to-end experience after core flows are stable.

Focus on:

- homepage;
- prediction interactions;
- match hub;
- pundit pages;
- leaderboards;
- receipts;
- content studio;
- account/subscription views;
- mobile-first navigation and responsive layout.

## Phase 14 - Production hardening

Perform:

- security review;
- rate limiting;
- validation;
- test completion;
- mobile/responsive testing;
- performance review;
- analytics;
- logging;
- job recovery;
- AI cost controls;
- Stripe webhook tests.

## Future phase - Multi domestic leagues

Only after Premier League validation:

- enable La Liga, Serie A, Bundesliga, Ligue 1 etc.;
- add competition selector;
- add user competition preferences;
- add competition-specific leaderboards;
- personalize feeds and pundit tracking by selected competitions.

International and cup competitions remain a separate future plan.
