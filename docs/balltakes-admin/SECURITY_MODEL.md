# Admin Security Model

## Authorization layers

Admin access is checked at four points. Only the second one is authoritative.

1. **Frontend navigation.** `AppShell` renders the Admin link only when
   `session.isPlatformAdmin` is true. Cosmetic.
2. **Backend policy.** The `"Admin"` authorization policy is applied to the entire
   `/api/admin` group in `AdminEndpoints.MapAdminEndpoints`. `AdminAuthorizationHandler`
   delegates to `IAdminAuthorizationService`. This is the real gate.
3. **Route guard.** `frontend/src/proxy.ts` verifies that a valid Supabase session
   exists before serving `/admin/*`. It cannot check the admin role because the role is
   not a JWT claim, so it only stops unauthenticated visitors. UX only.
4. **Shell guard.** `AdminShell` redirects to `/` when the session reports a
   non-admin. UX only; the pages it wraps have no data of their own because every
   admin fetch goes through an authorized API call.

Because layers 1, 3 and 4 are advisory, a non-admin who forces their way to `/admin`
sees an empty shell and every API call returns 403.

## Role source

`users.IsPlatformAdmin` is the authoritative role flag. It is a server-managed column
in the application database, not Supabase user metadata, so it cannot be edited by the
user it describes.

`AdminAuthorizationService` grants admin if any of the following hold:

- the caller's user ID appears in `Admin:AllowedUserIds`;
- the caller's JWT `email` claim appears in `Admin:AllowedEmails`;
- `users.IsPlatformAdmin` is true for the caller.

The first two also promote the user by setting `IsPlatformAdmin = true`, which is how
the first admin is bootstrapped.

A role value sent by the browser is never consulted.

## Permissions

`AdminPermissions` defines named capabilities:

```
Admin.Analytics.View   Admin.Users.View     Admin.Users.Manage
Admin.Jobs.View        Admin.Jobs.Execute   Admin.Cache.View
Admin.Cache.Invalidate Admin.Audit.View
```

`IAdminAuthorizationService.HasPermissionAsync` currently resolves every permission to
the platform-admin check. The seam exists so a permission table can be introduced later
without editing endpoint signatures. Endpoints that need a capability call
`HasPermissionAsync` rather than re-deriving admin status.

## First-admin bootstrap

Set the allowlist in the backend environment before the account signs in:

```
Admin__AllowedEmails__0=you@example.com
```

or, if the Supabase user ID is already known:

```
Admin__AllowedUserIds__0=00000000-0000-0000-0000-000000000000
```

Then register or log in normally through `/auth/register`. The first authorized request
promotes the row in `users` and writes `IsPlatformAdmin = true`.

The allowlist can be removed afterwards; the database flag persists. Keeping it is also
safe and provides a recovery path if the flag is ever cleared.

The first registered user is never made an admin automatically. There is no hardcoded
admin credential anywhere in source control.

## Supabase service-role key

`Supabase:ServiceRoleKey` backs `SupabaseAdminClient`, which is the only code permitted
to call `/auth/v1/admin/*`.

Rules enforced:

- server-only; it is read from `IOptions<SupabaseOptions>` inside the .NET API;
- never named with a `NEXT_PUBLIC_` prefix and never referenced in `frontend/`;
- never returned in an API response;
- never logged, including on failure paths, which log only status codes;
- never written to `admin_audit_logs` or `analytics_events`.

When the key is absent the client reports `IsConfigured = false` and user management
degrades to local database data instead of throwing.

## Privileged action audit

`IAdminAuditService.LogAsync` writes to `admin_audit_logs` with metadata passed through
`SecretSanitizer.SanitizeJson`. Actions recorded by this workstream:

| Action | Target type |
| --- | --- |
| `user.role.grant` | `user` |
| `user.role.revoke` | `user` |
| `user.status.change` | `user` |
| `job.run`, `job.pause`, `job.resume`, ... | `job` |
| `error.investigate`, `error.resolve`, ... | `operational_error` |
| `review.approve`, `review.reject`, ... | `pundit_opinion` |
| `football.sync.*` | `football-data` |

Audit rows are readable at `/admin/audit` and are never mutable or deletable from the
admin UI.

## Guards on role changes

- An admin cannot revoke their own admin role. Prevents accidental self-lockout.
- The last remaining admin cannot be demoted. Prevents total lockout.
- Account status cannot be changed on a platform admin without first revoking the role.
- Account deletion is deliberately not exposed. It requires the data-lifecycle
  workflow described in kit phase 10.

## Rate limiting

Admin mutations reuse the existing named policies in `RateLimitPolicies`. Role and
status changes use `admin.users.manage` (10 per minute). Analytics ingestion uses
`analytics.ingest` (60 per minute), partitioned by user or anonymous ID before IP.

## Acceptance criteria

- An anonymous request to any `/api/admin/*` route returns 401.
- An authenticated non-admin receives 403.
- Editing browser local state or session storage does not grant access.
- A forged role field in a request body is ignored; the server reads only its own table.
- The service-role key does not appear in the client bundle.
- Every privileged mutation produces an audit row.
