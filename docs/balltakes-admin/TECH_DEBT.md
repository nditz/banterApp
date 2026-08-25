# Technical Debt

Items found during the Phase 0 audit or created knowingly during the admin core
workstream. Each entry states the risk and what closing it requires.

## T1 — Hangfire uses in-memory storage

`Program.cs` configures `UseInMemoryStorage()`. Recurring schedules, retry state and
in-flight jobs are lost on every restart or redeploy, which on Render happens routinely.

Mitigated in part: pause and enable state is separately persisted in
`job_registry_state`, so admin intent survives. Hangfire's own view does not.

Close by switching to `Hangfire.PostgreSql` against the existing connection string and
verifying that the schema is created in a dedicated `hangfire` schema rather than
`public`, so the blanket RLS enable does not affect it.

## T2 — Guest to account claim is not implemented

`docs/balltakes-premier-league-cursor-kit/05_AUTH_GUESTS_AND_USERS.md` describes the
flow and the product README implies it, but no code exists. A guest who registers loses
access to their predictions, league memberships, bonus picks and generated content.

Close with an idempotent merge keyed on the recovery token, reassigning
`AnonymousUserId` rows to `UserId` across `predictions`, `league_members`,
`matchweek_bonuses`, `tournament_bonus_picks` and `generated_content`, guarding against
double-membership in the same league, and leaving the guest row inert.

## T3 — No password reset

`RateLimitPolicies.AuthPasswordReset` exists and is wired into the limiter, but no route
or UI uses it. Users who forget a password have no in-product recovery path.

Close with a Supabase `resetPasswordForEmail` call plus an update-password page behind
the existing callback route.

## T4 — Account deletion has no safe workflow

Only `AccountStatus` transitions are exposed. See `DECISIONS.md` D7. A real deletion
request currently requires manual database work.

Close as part of kit phase 10, after deciding per-table whether to delete or anonymize.

## T5 — `anonymous_users.RecoveryCode` is written but never read

`GenerateRecoveryCode()` populates a 12-character hex column on consent, but recovery
validation uses the HMAC token from `SessionTokenService` instead. The column is dead
weight that looks like a credential.

Close by dropping the column, or by documenting it as a deliberate fallback if one is
intended. Do not leave it ambiguous, since it reads as a secret during security review.

## T6 — Divergent legacy Supabase SQL schema

`supabase/migrations/*.sql` defines a `profiles` table and PostgREST RLS policies from
an earlier direct-to-Supabase design. The live schema uses EF Core and a `users` table.
A newcomer running the SQL migrations would create tables the application never reads.

Close by deleting the directory or adding a prominent header marking it superseded.
`supabase/README.md` partially covers this but the files themselves do not.

## T7 — Dead Prisma dependency

`frontend/package.json` depends on `prisma` and `@prisma/client`; `frontend/prisma/schema.prisma`
is an empty stub with no models and zero usage in source. It adds install time and
implies a data-access path that does not exist.

Close by removing both dependencies and the directory.

## T8 — `ContentSourceOptions.CacheDuration` is configured but never read

Present in `appsettings.json` and bound to options, referenced nowhere. Suggests caching
exists where it does not.

Close alongside kit phase 8, either by wiring it into `ICacheService` or removing it.

## T9 — `/admin` route guard cannot check role

`frontend/src/proxy.ts` can verify that a Supabase session exists but cannot determine
admin status, because the role lives in the application database rather than the JWT.
A signed-in non-admin therefore reaches the admin shell before being redirected by
`AdminShell`.

This is acceptable because no admin data is rendered without an authorized API call, but
it is a worse experience than a clean server-side redirect. Closing it properly requires
either a custom access token hook that adds a role claim, or an edge call to the backend
on every `/admin` navigation. Neither is worth the cost yet.

## T10 — Analytics dashboards do not exist

The pipeline stores events but nothing reads them yet, so the table grows without
delivering value until kit phase 6 ships. Retention cleanup keeps it bounded in the
meantime.

## T11 — Identifier casing is split between tables and columns

`AppDbContext` names every table explicitly in snake_case via `ToTable("users")`, but
column names are left to the EF default and come out PascalCase (`"EventName"`,
`"OccurredAt"`). Postgres folds unquoted identifiers to lower case, so hand-written SQL
must quote every column while table names need no quoting — an inconsistency that reliably
costs a few minutes in psql and the Supabase SQL editor.

Closing it means adding `EFCore.NamingConventions` and `UseSnakeCaseNamingConvention()`,
which renames every column in the database and needs a migration plus a sweep of any raw
SQL. Cosmetic, so not worth scheduling on its own; fold it into the next migration that
already touches most tables.

## T12 — `DotNetEnv` is referenced but never used

`BanterApp.Api.csproj` takes a `DotNetEnv` 3.2.0 dependency and no source file calls
`Env.Load`. Configuration comes entirely from `appsettings*.json` and real environment
variables. The dependency implies the API reads a `.env` file at startup, which it does
not — a misleading signal when debugging why a local value is not being picked up.

Close by removing the `PackageReference`.
