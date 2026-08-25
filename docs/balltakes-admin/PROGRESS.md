# Progress

Updated as each work item completes. Work is only marked done after build, lint and
tests pass, not when code is written.

## Admin core workstream

| # | Work item | Kit phase | Status |
| --- | --- | --- | --- |
| 1 | Working documents | 0 | Done |
| 2 | Authorization hardening | 2 | Done |
| 3 | User management backend | 3 | Done |
| 4 | User management frontend | 3 | Done |
| 5 | Audit log filters and page | 2 / 3 | Done |
| 6 | Consent backend | 4 | Done |
| 7 | Consent banner and gating | 4 | Done |
| 8 | Analytics pipeline backend | 5 | Done |
| 9 | Analytics client instrumentation | 5 | Done |
| 10 | Verification | 11 partial | Done |

## What shipped

**Authorization.** `AdminPermissions` constants plus `HasPermissionAsync` on
`IAdminAuthorizationService`. The `/admin` proxy guard now verifies a real Supabase
session instead of merely checking for a cookie whose name starts with `sb-`.

**User management.** `SupabaseAdminClient` calls `/auth/v1/admin/users` with the
service-role key, server-side only, and degrades to local data when the key is absent.
`AdminUsersService` merges identity with local role, status and activity counts. Five
endpoints under `/api/admin/users`, all audited, with self-demotion and last-admin
guards. Pages at `/admin/users` and `/admin/users/[userId]`.

**Audit.** `/api/admin/audit-logs` gained `action`, `adminUserId`, `from`, `to`,
`page` and `pageSize` with clamped bounds and a total count. Read-only page at
`/admin/audit`.

**Consent.** `consent_preferences` table keyed by user or anonymous session.
`GET`/`POST /api/consent`. A banner with equally weighted Accept and Reject, nothing
pre-ticked, reopenable from the footer and the privacy page. AdSense is now gated on
marketing consent, closing a live compliance defect.

**Analytics.** `analytics_events` table, a server-side event catalog that rejects
uncatalogued names and drops uncatalogued properties, a consent-gated ingest endpoint
that always returns 202, a typed buffered client tracker that no-ops without consent,
and a daily `analytics.retention.cleanup` job.

## Verification

| Check | Command | Result |
| --- | --- | --- |
| Backend unit and integration tests | `dotnet test BanterApp.Api.Tests` | 254 passed, 0 failed |
| Frontend unit tests | `npm test` | 16 passed |
| Frontend typecheck and build | `npm run build` | Compiled, 34 routes generated |
| Frontend lint | `npm run lint` | 0 errors (1 pre-existing warning in `admin/error.tsx`) |
| Service-role key absent from client bundle | grep `.next/static` for `service_role`, `serviceRoleKey`, `SERVICE_ROLE` | No matches |

New coverage added in this workstream:

- `Admin/AdminUsersServiceTests.cs` — self-demotion and last-admin demotion refused,
  suspending an admin refused until the role is removed, unknown status rejected, page
  size clamped, search matches email and display name case-insensitively, and the
  degraded identity source is reported when the service-role key is absent.
- `Admin/AdminUsersEndpointTests.cs` — anonymous and authenticated non-admin requests to
  `/api/admin/users` and `/api/admin/audit-logs` denied, mutations without a CSRF token
  refused, a granted role writes an `user.role.grant` audit row attributed to the acting
  admin, a refused mutation writes none, and the audit endpoint honours the action filter,
  page size and inverted date-range rejection.
- `Analytics/AnalyticsIngestEndpointTests.cs` — an uncatalogued event name is rejected
  with 400 and stores nothing, a batch without consent is accepted but stores nothing,
  a consented batch stores only allowlisted properties, and an oversized batch is
  rejected. A catalog test asserts no property key can carry identifying or free-form
  content.
- `Analytics/ConsentEndpointTests.cs` — consent defaults to nothing granted, is
  persisted and read back, updates in place rather than duplicating, and withdrawal
  immediately stops analytics ingest.
- `frontend/src/lib/consent.test.ts` — an absent, stale-version or malformed record never
  implies consent; the store snapshot stays referentially stable.
- `frontend/src/lib/analytics/analytics.test.ts` — `track()` makes no request without
  consent, batches once granted, discards its buffer on withdrawal, and truncates
  oversized property values.
- `e2e/responsive/admin-pages.spec.ts` — `/admin/users` and `/admin/audit` added to the
  responsive sweep, checked for their filter controls, asserted to expose no destructive
  action, and confirmed inaccessible when unauthenticated.

## Deferred

Analytics dashboards (phase 6), caching and cache admin (phases 8 and 9), privacy and
user-rights operations (phase 10), the full production hardening sweep (phase 11),
password reset, guest-to-account claim, and durable Hangfire storage. See
`MASTER_PLAN.md` and `TECH_DEBT.md`.

## Operator action required

`Supabase__ServiceRoleKey` must be set in the backend environment on Render and in local
`appsettings.Development.json` before `/admin/users` shows Supabase identity data such
as last sign-in and provider. Until then the page works but reports
`identitySource: "database"` and displays a warning.
