# Analytics Model

First-party only. No third-party analytics vendor is used, so there is no processor
agreement, no cross-site identifier and no data leaving the existing infrastructure.

## Storage

Events land in `analytics_events` (EF entity `AnalyticsEvent`).

| Column | Notes |
| --- | --- |
| `Id` | uuid |
| `EventName` | validated against the server-side catalog |
| `OccurredAt` | UTC, server-assigned |
| `UserId` | nullable; set only for authenticated callers |
| `AnonymousSessionId` | nullable; the existing guest UUID |
| `Feature` | coarse product area |
| `PropertiesJson` | small, allowlisted, sanitized |
| `AppVersion` | nullable |
| `Environment` | `Development` / `Production` |
| `CountryCode` | nullable, two letters, from the existing header |

Indexed on `(EventName, OccurredAt)` and `(OccurredAt)` so the deferred dashboards can
aggregate by event and by day, and so retention cleanup can scan by date.

Deliberately **not** stored: IP address, user agent, referrer, precise location,
free-form text, prompt or model output content.

## Event catalog

`backend/.../Features/Analytics/AnalyticsEventCatalog.cs` is the single source of truth.
It maps each permitted event name to a feature and to the set of property keys that may
accompany it.

An event whose name is not in the catalog is rejected with 400. A property key that is
not in the catalog entry is silently dropped rather than failing the batch, so a
frontend deploy that adds a field cannot break ingestion.

Property values are additionally constrained: strings are truncated, only primitives are
accepted, and every value passes through `SecretSanitizer`.

Catalogued events, grouped by feature:

**acquisition** — `session_started`, `landing_viewed`, `guest_session_created`,
`recovery_key_created`

**auth** — `registration_started`, `registration_completed`, `login_completed`,
`guest_claim_completed`

**prediction** — `fixture_viewed`, `prediction_started`, `prediction_created`,
`prediction_updated`, `matchweek_predictions_completed`, `prediction_result_viewed`,
`leaderboard_viewed`

**league** — `prediction_league_created`, `prediction_league_joined`,
`prediction_league_viewed`

**pundit** — `pundit_list_viewed`, `pundit_profile_viewed`, `pundit_comparison_viewed`,
`pundit_source_opened`

**content** — `content_generation_started`, `content_generation_completed`,
`content_generation_failed`, `content_regenerated`, `content_exported`

`recovery_key_created` records that a key was generated. The key value is never sent.

## Consent gating

Ingestion is refused unless the caller has an `analytics` consent grant. The check
happens on the server, in `AnalyticsIngestService`, against `consent_preferences`. The
client also refuses to buffer or send anything without local consent, so refusal costs
zero network requests, but the server does not trust that.

A caller with no consent record at all is treated as not consented.

The response is always `202 Accepted` with a count of accepted events, whether or not
events were dropped. Analytics must never surface an error into a product flow.

## Client pipeline

`frontend/src/lib/analytics/` exposes `track(eventName, properties)`.

- The event name is typed against a mirror of the catalog, so an unknown name is a
  compile error rather than a runtime rejection.
- Calls are buffered in memory and flushed on a short timer, on reaching a batch size,
  and on `visibilitychange` to hidden.
- Without analytics consent, `track` returns immediately and the buffer stays empty.
- Withdrawal clears any pending buffer, so queued events are not sent afterwards.
- Failures are swallowed. Analytics never blocks or breaks a user flow.

## Operational data stays separate

API failures, job failures, provider failures and auth failures are **not** product
analytics. They continue to flow through `IErrorTrackingService` into `errors` and
through `SyncRunTracker` into `sync_runs`. That data is strictly necessary for operating
the service and is not consent-gated.

## Retention

`Analytics:RawEventRetentionDays` (default 180) controls how long raw rows live. The
`analytics.retention.cleanup` job deletes rows older than the window on a daily
schedule. The value is read from configuration in one place, never inlined into
business logic.

Aggregated metrics, once the deferred dashboards exist, may be retained longer because
they are no longer personal.

## Metrics this model can answer

Because events are aggregated server-side, admin dashboards will be able to report
acquisition, activation, prediction engagement, league participation, pundit
engagement and AI content usage without exposing raw rows to the browser.

Metrics that do not need events at all — registered user counts, prediction volume,
league counts, generation counts — are already derivable from domain tables and should
be computed there rather than from `analytics_events`, so they remain correct for users
who decline analytics.
