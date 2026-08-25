# Caching Strategy

## Goal

Reduce unnecessary external API/database calls and improve page responsiveness without serving dangerously stale or user-inappropriate data.

## First principle

Do not cache something merely because it is easy.

For every cache candidate document:

- source of truth;
- cache key;
- expected freshness;
- TTL;
- invalidation event;
- fallback on cache failure;
- whether data is shared or user-specific.

## Good BallTakes cache candidates

### Long-lived / slow-changing

- competition metadata;
- team metadata;
- player profile metadata;
- pundit profile metadata;
- static configuration;
- season metadata.

### Medium-lived

- league standings;
- upcoming fixtures;
- recent results;
- player statistics;
- pundit leaderboard;
- public leaderboard snapshots;
- news/feed summaries.

### Short-lived

- homepage aggregate widgets;
- current matchweek public stats;
- aggregate prediction percentages.

## Generally avoid or be very careful caching

- authorization/role decisions;
- private user data in shared cache keys;
- recovery credentials;
- access tokens;
- Stripe secrets;
- admin-only data in public caches;
- rapidly changing live-match data without an explicit freshness strategy.

## Cache architecture

Inspect deployment topology before choosing implementation.

If the backend is a single persistent process and data volume is small, in-memory caching may be acceptable for selected data.

If multiple API instances/processes must share cache state, prefer a distributed cache such as Redis or the platform's existing distributed caching capability.

Do not introduce Redis purely for fashion if the deployed architecture does not benefit from it.

## Recommended pattern

Use a reusable cache service/decorator rather than scattered direct cache calls.

Conceptually:

`ICacheService`

- `GetAsync<T>`
- `SetAsync<T>`
- `GetOrCreateAsync<T>`
- `RemoveAsync`
- `RemoveByTag/GroupAsync` if supported

Or use the established .NET caching abstraction already present.

## Key design

Use deterministic namespacing:

`balltakes:{environment}:{domain}:{version}:{identifier}`

Examples:

`balltakes:prod:standings:v1:premier-league:2026-27`

`balltakes:prod:fixtures:v1:matchweek:3`

Avoid embedding PII in cache keys.

## Invalidation

Examples:

- standings sync completes -> invalidate standings/home widgets;
- fixtures sync completes -> invalidate fixture/matchweek caches;
- result sync completes -> invalidate results, standings, public stats;
- pundit scoring completes -> invalidate pundit leaderboard;
- admin edits pundit -> invalidate pundit profile/list;
- news sync completes -> invalidate feed/news cache.

## Cache stampede protection

For expensive shared queries, consider single-flight/locking so many simultaneous requests do not all refresh the same expired key.

## Failure behavior

Cache must be an optimization, not the only copy of business data.

If cache is unavailable:

- application should generally fall back to source of truth;
- log operational warning;
- avoid cascading failure;
- do not silently return incorrect authorization results.

## Admin controls

Admin may:

- view cache groups/freshness;
- invalidate known keys/groups;
- request refresh through safe domain services.

Admin must NOT receive a raw Redis terminal or arbitrary cache-key execution interface.

## Testing

Test:

- cache hit;
- cache miss;
- expiry;
- invalidation after data sync;
- cache provider unavailable;
- no cross-user leakage;
- no admin/private data served through public cache.
