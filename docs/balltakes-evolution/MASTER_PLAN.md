# Master Plan — Premier League core product

Source of truth for the BallTakes World Cup → Premier League pivot. Later kit phases (pundits, receipts, Stripe, multi-league) are deferred.

## Product loop

Predict the Premier League. Beat your mates. Come back next matchweek.

## In scope

1. Remove World Cup as a live product: UI, copy, sync IDs, bracket engine, and WC rows.
2. Domain: Competition → CompetitionSeason → Matchweek → Fixture (Match). Friend groups stay as `leagues`.
3. Sync Premier League 2026/27 via API-Football league `39`, season `2026`.
4. Matchweek prediction flow, kickoff lock, rescore on full time, perfect-matchweek bonus.
5. Private leagues scored on the Premier League season.
6. One season-awards board replacing tournament bonuses and WC country picks.
7. Premier League homepage, table, nav, rules, legal, SEO.

## Out of scope

- Me vs Pundits product surface
- Structured receipts and premium AI meme pipeline
- Stripe credits / subscriptions
- Competition selector / other domestic leagues
- Cups, Champions League, future World Cups
- Preserving World Cup history (explicitly wiped)

## Target routes

| Route | Role |
| --- | --- |
| `/` | Current matchweek hub |
| `/matchweek` | Full pick board |
| `/table` | Premier League standings |
| `/awards` | Season-long picks |
| `/leagues` | Friend leagues |
| `/studio`, `/rules`, `/auth/*` | Kept, copy updated |
| `/brackets`, `/bonuses`, WC `/predictions/*` | Removed with redirects |

## Done when

- No live World Cup UX, copy, or fixture data
- Current PL matchweek is predictable end-to-end
- Results score without the user re-saving a pick
- Private leagues rank a Premier League season
- Season awards exist and lock
- Site reads as a Premier League product on mobile and desktop
