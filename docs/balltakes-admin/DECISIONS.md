# Architecture Decisions

## D1 — First-party analytics rather than a vendor

**Context.** No analytics existed. The kit forbids advertising trackers and prefers
EU-friendly, privacy-preserving configuration.

**Decision.** Store events in a first-party `analytics_events` table behind a .NET
ingest endpoint, with a server-side event catalog.

**Rationale.** No processor agreement, no data residency question, no third-party script
to gate, and no cross-site identifier. The volume BallTakes generates is trivial for
Postgres. The alternative, PostHog EU, would add a vendor relationship for capability
the product does not yet need.

**Consequence.** Dashboards must be built rather than obtained. Retention is our
responsibility, handled by `analytics.retention.cleanup`.

## D2 — Keep `IsPlatformAdmin`, add a permission seam

**Context.** The kit suggests a `user_roles` table with a Supabase custom access token
hook. The repository already has a boolean column and a working policy.

**Decision.** Keep `users.IsPlatformAdmin` as the authority. Add `AdminPermissions`
constants and `HasPermissionAsync`, which currently resolves every permission to the
platform-admin check.

**Rationale.** A single-admin deployment does not benefit from granular roles today, and
a token hook would put role data in the JWT where it goes stale until refresh. The seam
means adding a permission table later touches one service, not every endpoint.

**Consequence.** All admins are equal for now. `HasPermissionAsync` calls in endpoints
look redundant until the table exists; that is deliberate.

## D3 — No Supabase anonymous identities

**Context.** Supabase supports anonymous users, which could in principle replace the
first-party recovery-key model.

**Decision.** Keep the existing `anonymous_users` plus HMAC recovery token flow.

**Rationale.** Migrating would invalidate recovery keys already issued to real users,
and the current model carries device-fingerprint rebinding that Supabase does not
provide. The kit explicitly says to inspect and preserve the existing implementation.

**Consequence.** Two identity systems coexist, joined only at claim time, and the claim
step still has to be built.

## D4 — Consent stored server-side, mirrored client-side

**Context.** Guests have no account, so consent has nowhere obvious to live.

**Decision.** Persist `consent_preferences` keyed by either `UserId` or
`AnonymousUserId`, and mirror the decision into a `banter_consent` localStorage entry.

**Rationale.** The server copy is authoritative and lets the ingest endpoint reject
non-consented events. The client mirror means the banner does not flash on every page
load and `track()` can no-op without waiting for a round trip.

**Consequence.** The two can diverge if storage is cleared. The server always wins, and
a cleared mirror simply re-shows the banner.

## D5 — Marketing consent gates AdSense

**Context.** AdSense loaded unconditionally from the root layout with no banner. This
was a live compliance defect, not a new requirement.

**Decision.** Introduce a `marketing` consent category, default off, and gate the
AdSense script on it.

**Rationale.** Advertising cookies require prior consent. The kit forbids adding
advertising infrastructure, but this is existing infrastructure being brought under
control rather than new tracking.

**Consequence.** Ad impressions will fall for users who decline. That is the correct
outcome. `AdSlot` renders a neutral placeholder instead of an empty box.

## D6 — Analytics ingest always returns 202

**Context.** An ingest endpoint that returns errors invites client retry loops and can
surface failures into product UI.

**Decision.** Accept the batch, drop what is invalid or non-consented, and always
respond `202` with an accepted count. Only a malformed request body or an uncatalogued
event name produces `400`.

**Rationale.** Analytics must never break a user flow. Reporting counts keeps the
behaviour debuggable without making failure actionable on the client.

**Consequence.** Silent drops are possible. The accepted count in the response and the
server log make them observable.

## D7 — No account deletion endpoint yet

**Context.** The kit lists user deletion as a candidate admin action.

**Decision.** Ship `AccountStatus` changes (`Active`, `Suspended`, `Banned`) and no
delete.

**Rationale.** Deleting a Supabase auth user leaves predictions, league memberships,
leaderboard entries and generated content orphaned, and silently corrupts historical
standings. A safe workflow needs the anonymize-versus-delete decision from kit phase 10.

**Consequence.** A deletion request currently requires a documented manual process.
Recorded in `TECH_DEBT.md`.

## D8 — Cache deferred, design recorded

**Context.** Kit phases 8 and 9 call for a caching layer.

**Decision.** Do not implement caching in this workstream. Record the intended groups,
TTLs and invalidation triggers in `CACHING_MODEL.md`.

**Rationale.** Caching is independent of admin core and carries its own correctness
risk. Shipping it alongside four other subsystems would make regressions hard to
attribute.

**Consequence.** Repeated public reads still hit the database. Acceptable at current
traffic.

## D9 — The client event catalog is a hand-maintained mirror

**Context.** `AnalyticsEventCatalog.cs` is the authority for event names and permitted
property keys. The client needs the same list to get compile-time safety.

**Decision.** Keep `frontend/src/lib/analytics/events.ts` as a hand-written mirror rather
than generating it from the backend or sharing a schema package.

**Rationale.** The catalog changes rarely and deliberately — every addition is a privacy
decision that should be reviewed on both sides. A generator would add a build step and a
cross-project dependency to keep roughly thirty string literals in sync.

**Consequence.** The two can drift. The server is authoritative, so drift shows up as a
rejected event rather than as leaked data, and
`AnalyticsIngestEndpointTests.PostEvents_WithUnknownEventName_ReturnsBadRequest` pins the
failure mode.

## D10 — Consent is read through `useSyncExternalStore`

**Context.** The first `useConsent` implementation read `localStorage` in an effect and
called `setState`, which the React compiler lint rules reject as a cascading render.

**Decision.** Expose `subscribeToConsent` / `getConsentSnapshot` from `lib/consent` and
consume them with `useSyncExternalStore`. The server snapshot reports `ready: false`.

**Rationale.** `localStorage` plus the `banter:consent-changed` event is exactly an
external store. This also makes cross-tab changes propagate for free via the `storage`
event, which the effect-based version did not handle.

**Consequence.** `getConsentSnapshot` must return a referentially stable object, so the
module caches the parsed record against the raw stored string. A test pins that
behaviour, because losing it would cause an infinite render loop rather than a visible
bug.
