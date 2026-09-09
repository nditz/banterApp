# Matchweek UI

## Header
- Matchweek number
- date range
- picks progress
- countdown/deadline

## Fixture card
- competition/matchweek context
- home crest/name
- away crest/name
- kickoff
- pick mode tabs: Result / Exact score / Double chance
- lock state
- pundit comparison teaser when available

## Interaction
Selecting a pick should feel immediate.
Use subtle motion/check state and optimistic UI where safe.

## Returning user
Show `7 of 10 locked` and prioritize unfinished fixtures.

## Required states
Loading: skeleton fixture cards
Empty: explain no fixtures available + next refresh/retry
Error: friendly retry + preserve existing picks
Loaded: full board
Locked: immutable/read-only after kickoff

## Mobile
- no cramped 3-column layouts
- generous tap targets
- sticky `Finish picks`/progress bar
