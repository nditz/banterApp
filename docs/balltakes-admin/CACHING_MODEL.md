# Caching Model

Status: **not implemented**. Kit phases 8 and 9 are deferred. This document captures the
audit findings and the intended design so the follow-up workstream starts from a
decision rather than a blank page.

## What exists today

| Layer | Mechanism | Location |
| --- | --- | --- |
| HTTP | `Cache-Control: public, max-age=60` on the feed | `Features/Feed/FeedEndpoints.cs` |
| In-process | GIF search results, bounded dictionary | `GiphyGifProvider`, `TenorGifProvider` |
| In-process | OpenFootball fixtures, 15-minute TTL | `OpenFootballProvider` |
| Client | TanStack Query `staleTime` 30-60s | `frontend/src/hooks/*` |
| Dead config | `ContentSourceOptions.CacheDuration` | never read |

There is no `IMemoryCache`, no `IDistributedCache`, no Redis, and no Next.js
`revalidateTag` or `use cache` usage.

## Topology constraint

The API is a single Render web service. A distributed cache would add an operational
dependency without a correctness benefit at the current instance count. The kit is
explicit that Redis should not be introduced for fashion.

Decision: implement `ICacheService` over `IMemoryCache` with tag-based grouping, behind
an interface that a Redis implementation can satisfy later. Record the switch point as
"more than one API instance" rather than a date.

## Intended cache groups

| Group | Source of truth | TTL | Invalidated by |
| --- | --- | --- | --- |
| `competition-metadata` | `competitions`, `competition_seasons` | 24h | manual |
| `team-metadata` | `club_teams`, `season_teams` | 24h | reference sync |
| `player-metadata` | `players` | 12h | `football.players.sync` |
| `standings` | `standing_rows` | 30m | `standings-sync` |
| `fixtures` | `matches` | 15m | `score-sync` |
| `results` | `matches` | 15m | `score-sync` |
| `public-leaderboard` | `leaderboard_entries` | 5m | scoring |
| `pundit-profiles` | `pundits` | 1h | admin edit, `rss.sync` |
| `feed-summaries` | `news_feed_items` | 5m | `news-ingest` |
| `prediction-aggregates` | `prediction_aggregates` | 10m | `predictions.aggregate.refresh` |

## Key format

```
balltakes:{environment}:{domain}:{version}:{identifier}
```

No personally identifying value ever appears in a key. Nothing user-specific is cached
in a shared key.

## Never cached

Authorization and role decisions, private user data in shared keys, recovery tokens,
access tokens, provider credentials, admin-only datasets, and live match state without
an explicit freshness bound.

## Failure behaviour

The cache is an optimisation, never the only copy. On a cache miss or a cache backend
failure the request falls through to the source of truth and logs an operational
warning. A cache outage must not change an authorization outcome.

## Admin controls (phase 9)

`GET /api/admin/cache/groups` and
`POST /api/admin/cache/groups/{groupKey}/invalidate`, restricted to the group keys in
the table above and audited. No raw cache-key or command interface is exposed to the
browser.
