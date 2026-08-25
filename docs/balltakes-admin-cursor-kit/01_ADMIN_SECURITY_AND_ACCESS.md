# Admin Security & Access Model

## Goal

Create an admin area that cannot be accessed or operated by normal authenticated users, anonymous users or guest/recovery-key sessions.

## Recommended authorization model

Use the existing authorization architecture where possible.

If no suitable model exists, implement:

- `UserRole` or equivalent application role mapping;
- `Admin` role;
- optional future permissions such as:
  - `Admin.Analytics.View`
  - `Admin.Users.View`
  - `Admin.Users.Manage`
  - `Admin.Jobs.View`
  - `Admin.Jobs.Execute`
  - `Admin.Cache.View`
  - `Admin.Cache.Invalidate`
  - `Admin.Audit.View`.

For an early single-admin deployment, role-based access is sufficient, but structure the policy checks so permissions can be added without rewriting controllers/pages.

## Supabase role propagation

Prefer one of these patterns based on repository reality:

1. server-managed application role table + Custom Access Token Hook adding an admin claim; or
2. backend lookup of application role after validating the Supabase user JWT.

Do not treat user-editable metadata as authoritative for admin access.

Do not trust a role value sent by the browser.

## Backend protection

Every `/admin` backend endpoint must require authenticated admin authorization.

Examples:

`GET /api/admin/overview`

`GET /api/admin/users`

`GET /api/admin/analytics/...`

`GET /api/admin/jobs`

`POST /api/admin/jobs/{jobKey}/run`

`GET /api/admin/cache`

`POST /api/admin/cache/{cacheKey}/invalidate`

`GET /api/admin/audit`

Use existing API conventions rather than introducing a parallel architecture.

## Frontend protection

- hide admin navigation for non-admins;
- use server/middleware route guards where supported;
- redirect unauthorized access to a safe page;
- do not render sensitive admin data and then hide it with CSS/client state;
- assume users can manually call URLs and APIs.

## Supabase Admin API

Any use of `supabase.auth.admin.*` must happen in a trusted server environment.

Never expose secret/service-role credentials through:

- Next.js public env variables;
- browser bundles;
- API responses;
- source maps;
- analytics events;
- frontend logs.

## Privileged action audit

Record an audit entry for actions such as:

- user disabled/deleted/invited;
- role changed;
- background job manually triggered;
- cache manually invalidated;
- analytics export generated;
- admin setting changed;
- subscription/credit adjustment later.

Suggested structure:

`AdminAuditLog`

- `Id`
- `AdminUserId`
- `Action`
- `TargetType`
- `TargetId`
- `MetadataJson` (sanitized)
- `OccurredAtUtc`
- `CorrelationId`

Do not log secrets or authentication tokens.

## Admin bootstrap

Provide a safe documented bootstrap method for the first admin account.

Examples:

- migration/SQL assignment by known Supabase user ID;
- protected server-side bootstrap command;
- manual role assignment through Supabase/database administration.

Do not automatically make the first registered user an admin.

## Security acceptance criteria

- anonymous visitor cannot load admin pages or APIs;
- normal logged-in user receives 401/403 as appropriate;
- modifying browser local state cannot grant admin access;
- forged role fields from the browser are ignored;
- admin secret keys never reach client code;
- all privileged operations have an audit trail;
- role changes take effect predictably after token refresh/session renewal.
