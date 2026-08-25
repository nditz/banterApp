# Supabase Auth & User Management

## Identity boundaries

| Owned by Supabase | Owned by BallTakes |
| --- | --- |
| credentials and password hashes | display name, avatar, country |
| OAuth identities | predictions and scores |
| sessions and tokens | league membership |
| email verification and reset flows | generated content history |
| sign-in timestamps and providers | `IsPlatformAdmin`, `AccountStatus` |

No password material is ever copied into BallTakes tables. The local `users` row is
keyed by the Supabase `sub` claim, so the two sides join on a single UUID.

## Flows

| Flow | Status | Location |
| --- | --- | --- |
| Email/password registration | Implemented | `frontend/src/app/auth/register/page.tsx` |
| Email/password login | Implemented | `frontend/src/app/auth/login/page.tsx` |
| Google OAuth | Implemented | same, `signInWithOAuth` |
| Email verification | Implemented | `auth/callback/route.ts`, `auth/confirm/route.ts` |
| Logout | Implemented | `AppShell` |
| Session restore | Implemented | `proxy.ts` plus `useSession` |
| Password reset | **Missing** | deferred |
| Guest to account claim | **Missing** | deferred |

After any successful sign-in the frontend calls `POST /api/auth/session/sync` with the
bearer token so the backend can upsert the local `users` row.

## JWT validation

Handled by `Microsoft.AspNetCore.Authentication.JwtBearer` with the symmetric
`Supabase:JwtSecret`. Issuer is `{Supabase:Url}/auth/v1`, audience is `authenticated`,
lifetime is validated, clock skew is two minutes. No cryptography is hand-rolled.

`SupabaseJwtMiddleware` projects the validated `sub` claim onto `IUserContext.UserId`.

## Anonymous and recovery-key flow

Preserved unchanged. Supabase anonymous identities are deliberately **not** adopted:
the existing HMAC recovery-token model already works, is device-fingerprint aware, and
migrating it would invalidate recovery keys currently held by real users. The tradeoff
is recorded in `DECISIONS.md`.

The still-missing piece is the claim step: a registered user cannot yet absorb their
guest history. The required properties when it is built are that the operation must be
idempotent, must not duplicate predictions, league memberships, bonus picks or
generated content, and must leave the guest row inert rather than deleting it.

## Admin user management

`SupabaseAdminClient` is the only component allowed to call `/auth/v1/admin/*`. It runs
in the API, authenticates with `Supabase:ServiceRoleKey` in both the `apikey` and
`Authorization` headers, and exposes exactly three operations: list users, get user by
ID, and a configuration probe.

`AdminUsersService` merges that identity data with the local `users` row and with
activity counts drawn from `predictions`, `league_members` and `generated_content`.

### Degraded mode

When `Supabase:ServiceRoleKey` is empty, `IsConfigured` is false. The service then
returns local database rows only, and the response carries
`identitySource: "database"` plus a warning so the admin UI can explain why sign-in
timestamps and providers are blank. Nothing throws.

### Endpoints

```
GET    /api/admin/users?page=&pageSize=&search=
GET    /api/admin/users/{userId}
POST   /api/admin/users/{userId}/roles          { "role": "admin" }
DELETE /api/admin/users/{userId}/roles/{role}
POST   /api/admin/users/{userId}/status         { "status": "Active" }
```

All require the `"Admin"` policy. Mutations additionally check
`AdminPermissions.UsersManage`, are rate-limited by `admin.users.manage`, and write an
audit row.

Responses never include tokens, password hashes, recovery codes, identity provider
secrets or raw Supabase payloads. Only an explicit projection is serialized.

### Not implemented on purpose

Account deletion and user invitation. Deletion requires the phase 10 data-lifecycle
decision about predictions, leaderboards and generated assets; deleting `auth.users`
alone would leave orphaned domain rows and corrupt historical leaderboards.
`AccountStatus` set to `Suspended` or `Banned` is the reversible alternative available
today.

## Service secret rule

`Supabase:ServiceRoleKey` is server-only. It is never prefixed `NEXT_PUBLIC_`, never
referenced under `frontend/`, never returned in a response, never logged, never written
to the database and never included in analytics. Verified as part of the build check.
