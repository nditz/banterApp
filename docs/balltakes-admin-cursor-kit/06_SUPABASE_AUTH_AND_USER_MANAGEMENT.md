# Supabase Auth & User Management

## Objective

Use Supabase Auth as the out-of-the-box identity system for permanent accounts while preserving BallTakes anonymous/recovery-key usage.

## Identity boundaries

Supabase owns:

- credentials;
- password authentication;
- OAuth identities;
- sessions/tokens;
- email verification/reset flows supported by Supabase.

BallTakes owns application/domain data such as:

- display name;
- username;
- avatar/application preferences;
- predictions;
- prediction leagues;
- pundit comparisons;
- content history;
- app role mapping;
- subscription/credits later.

Do not copy Supabase password hashes into BallTakes tables.

## Registration/login

Implement using Supabase-supported flows and existing frontend conventions.

Initial target:

- email/password registration;
- email/password login;
- email verification according to environment policy;
- password reset;
- Google OAuth if already part of product plan;
- logout;
- session refresh;
- route/session restoration.

## Application profile

Use an application profile table referencing `auth.users.id` or equivalent domain user model already introduced by BallTakes.

Example concept:

`UserProfile`

- `UserId` (Supabase user UUID)
- `Username`
- `DisplayName`
- `AvatarUrl`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Apply RLS where direct Supabase Data API access exists.

## Anonymous/recovery-key flow

Do not remove the existing recovery-key anonymous experience simply because Supabase also supports anonymous users.

First inspect the existing implementation and preserve user expectations/data compatibility.

Required journey:

1. visitor plays anonymously;
2. recovery mechanism remains available;
3. anonymous predictions/history are stored;
4. user later registers/logs in through Supabase;
5. guest session is claimed;
6. predictions, league memberships, scores and generated content are attached to permanent account;
7. operation is idempotent;
8. duplicate data is not created.

If using Supabase anonymous identities would materially simplify the current architecture, document the migration tradeoff before changing the established recovery-key model.

## Admin user management

Admin UI may retrieve/manipulate Supabase users through trusted backend code using Supabase Auth Admin APIs.

Implement server endpoints/services rather than calling Admin APIs directly from the browser.

Support only operations required by the product.

Candidates:

- paginated user list;
- inspect user identity/profile metadata needed for administration;
- invite user if useful;
- remove access/delete user through controlled workflow;
- app-role assignment;
- link to domain profile/activity.

## Service secret rule

Supabase secret/service-role keys:

- server only;
- never `NEXT_PUBLIC_*`;
- never return to client;
- never log;
- never store in database;
- never include in analytics.

## JWT validation

Backend APIs must validate Supabase-issued JWTs correctly using supported verification/JWKS/library mechanisms.

Do not implement cryptographic verification algorithms manually.

Use the token subject (`sub`) as the external user identity.

## Admin role

Prefer server-managed role assignment.

Possible implementation:

- `user_roles` table keyed by Supabase user ID;
- Custom Access Token Hook adds `user_role` claim;
- backend validates claim and/or authoritative role source;
- RLS uses role claim where appropriate.

Do not use user-editable metadata as the authorization authority.

## Account deletion

Create a controlled workflow that considers:

- Supabase Auth user deletion;
- application profile;
- predictions/history product requirements;
- generated assets/storage ownership;
- anonymous/pseudonymized historical leaderboards if required;
- privacy deletion/anonymization policy;
- active sessions/token expiry implications.

Do not simply delete `auth.users` and assume every application record is safely handled.

## Tests

Test:

- registration;
- login;
- invalid login;
- email verification path if enabled;
- password reset;
- OAuth callback where applicable;
- token expiry/refresh;
- guest claim;
- repeated guest claim;
- non-admin blocked from admin API;
- admin can list users server-side;
- service key absent from client bundle.
