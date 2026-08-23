# Premier League V1 - Definition of Done

Premier League V1 is not complete until the following end-to-end journey works reliably.

## Core user journey

1. User visits BallTakes.
2. User can continue anonymously.
3. Recovery mechanism is created securely.
4. User sees the current Premier League matchweek.
5. User predicts fixtures.
6. Predictions lock at the configured cutoff/kickoff.
7. Results synchronize from the football provider.
8. User predictions are scored correctly.
9. Matchweek and season leaderboards update.
10. Private prediction league standings update.
11. Tracked pundit predictions are scored with the same compatible rules.
12. User sees clear comparison with pundits.
13. Interesting outcomes create receipts.
14. User can create AI banter/content from a receipt.
15. User can export/share the result.
16. User can later create a Supabase account.
17. Guest history transfers without duplication or loss.
18. Paid upgrade through Stripe works.
19. Premium AI entitlement/credits are applied server-side.
20. AI usage is metered and cannot exceed limits silently.

## Data requirements

- Premier League competition record exists.
- Current season is configurable/data-driven.
- Matchweeks and fixtures sync safely.
- Teams are season participants rather than permanently hardwired to one league.
- External provider IDs are not domain primary keys.
- Sync jobs are idempotent.
- historical World Cup data is not unintentionally destroyed.

## Pundit requirements

- curated pundits exist;
- each published pundit prediction has provenance;
- low-confidence extracted predictions can be reviewed;
- pundits and users share compatible scoring logic;
- pundit leaderboard and user comparison work.

## Authentication requirements

- anonymous play works;
- recovery works;
- Supabase email/password works;
- Google OAuth works if configured;
- guest-to-account claim is idempotent;
- predictions/history are preserved.

## Monetization requirements

- plans are configurable;
- Stripe webhook handling is idempotent;
- browser success state cannot grant entitlement by itself;
- credit/usage transactions are auditable;
- AI endpoints enforce entitlement and rate limits.

## UX requirements

- mobile-first prediction flow is usable;
- homepage prioritizes matchweek, prediction progress and comparison;
- "Me vs Pundits" is prominent;
- receipts lead naturally into content creation;
- black/green BallTakes identity remains recognizable;
- World Cup-specific active-product copy is removed or isolated to history.

## Deferred and not required for V1

- multi-league selector;
- user league preferences;
- La Liga/Serie A/Bundesliga/Ligue 1 activation;
- international tournaments;
- brackets/group stages;
- domestic/European cup progression;
- direct social publishing APIs;
- full short-video pipeline;
- exhaustive pundit coverage.

## Product success test

The experience should make this proposition obvious:

"Predict the Premier League. Beat your mates. Beat the pundits. Turn the receipts into content."
