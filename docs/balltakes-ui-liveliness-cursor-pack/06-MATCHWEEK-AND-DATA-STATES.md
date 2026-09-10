# Matchweek, Fixtures and Data States

## Matchweek is content input
Prediction UI must be fast enough that users generate the raw material for receipts/Studio without friction.

## Fixture card
Show competition/matchweek context, kickoff, crests, teams, prediction state, lock state and optional pundit consensus teaser. Primary action is the prediction itself.

## Required states
Every football-data component must explicitly implement:
1. loading/skeleton
2. loaded
3. legitimate empty
4. stale/cached
5. provider/data error
6. offline/retry where applicable

Blank containers are forbidden.

## Stale-data UX
If cached fixtures are valid, keep them visible and show subtle freshness status. Do not replace useful cached content with a full-page error because a provider refresh failed.

## First prediction
After a user picks, reinforce the future payoff: `Locked. We'll keep the receipt.` and optionally surface followed-pundit calls when available.
