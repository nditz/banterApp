# Tech debt

## Deferred from this redo

- Pundit leaderboard and Me vs Pundits UX
- Structured receipts domain
- Stripe entitlements and AI credit ledger
- Multi-competition selector
- Direct social publishing
- Full short-video pipeline
- Splitting `tournament_bonus_picks` into a dedicated `season_award_picks` table name
- Replacing denormalized match team strings with Team FKs only

## Known follow-ups

- Guest-to-account claim audit (exists; not rebuilt here)
- News/YouTube relevance once PL sync is live in production
- Player-of-the-season official results still need an admin announcement path (award results table)
