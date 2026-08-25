# Current State — Repository Audit

Audit date: 2026-08-25. Corresponds to Phase 0 of `docs/balltakes-admin-cursor-kit/08_PHASED_IMPLEMENTATION.md`.

## Architecture

BallTakes is a two-tier application, not a single Next.js app.

```
Browser
  |
  |-- Supabase Auth (JS SDK, cookie sessions via @supabase/ssr)
  |
  v
Next.js 16 App Router (frontend/)              deployed to Vercel
  |
  |-- rewrite /api-backend/* -> API_PROXY_URL
  v
ASP.NET Core 9 Minimal API (backend/)          deployed to Render (Docker)
  |
  |-- EF Core / Npgsql
  v
Supabase PostgreSQL (session pooler, port 5432)
```

The frontend never talks to Supabase PostgREST. Supabase is used purely as an identity
provider plus a hosted Postgres instance. All domain data flows through the .NET API.

| Layer | Technology | Location |
| --- | --- | --- |
| Frontend | Next.js 16.3.2, React 19, TypeScript, Tailwind 4, TanStack Query v5 | `frontend/` |
| Backend | ASP.NET Core 9 Minimal API, EF Core, Hangfire | `backend/BanterApp.Api/` |
| Database | Supabase PostgreSQL | EF Core migrations |
| Auth | Supabase Auth (email/password + Google OAuth) | both tiers |

## Authentication map

Two parallel identity concepts coexist.

### Permanent accounts (Supabase)

- Registration/login happen in the browser through `@supabase/ssr`
  (`frontend/src/app/auth/register/page.tsx`, `frontend/src/app/auth/login/page.tsx`).
- After a session exists, the frontend calls `POST /api/auth/session/sync` with the
  bearer token; the backend upserts a row in the local `users` table keyed by the
  Supabase `sub` claim.
- The backend validates the JWT with `Microsoft.AspNetCore.Authentication.JwtBearer`
  using the symmetric `Supabase:JwtSecret`, issuer `{Supabase:Url}/auth/v1`,
  audience `authenticated` (`backend/BanterApp.Api/Program.cs`).
- `SupabaseJwtMiddleware` maps `sub` onto `IUserContext.UserId`.
- OAuth callback handlers live at `frontend/src/app/auth/callback/route.ts` (PKCE)
  and `frontend/src/app/auth/confirm/route.ts` (token hash).

`backend/.../Features/Auth/AuthEndpoints.cs` also exposes server-side
`POST /api/auth/register` and `POST /api/auth/login`. The frontend does not use them.

### Guest sessions (first-party)

- The browser generates a UUID and stores it in the `banter_anonymous_id` cookie plus
  `localStorage` (`frontend/src/lib/anonymous.ts`), sent as the `X-Anonymous-Id` header.
- `AnonymousUserMiddleware` resolves that ID against the `anonymous_users` table when
  no JWT is present.
- `POST /api/auth/session/consent` accepts terms, creates the `anonymous_users` row and
  issues an HMAC-signed recovery token (`SessionTokenService`, prefix `banter.v1`,
  365-day validity, signed with `Security:SessionSecret`).
- `POST /api/auth/session/recover` validates the token signature and rebinds cookies,
  rotating `CookieId` when the device fingerprint differs.

Guests can predict, join leagues, pick bonuses and set a username.

## Admin / security map

- Authorization policy `"Admin"` is applied to the whole `/api/admin` group in
  `backend/.../Features/Admin/AdminEndpoints.cs` via `.RequireAuthorization("Admin")`.
- `AdminAuthorizationService` grants admin when the user ID is in
  `Admin:AllowedUserIds`, the JWT email is in `Admin:AllowedEmails`, or
  `users.IsPlatformAdmin` is true. Allowlisted users are auto-promoted, which is the
  bootstrap mechanism.
- `AdminAuditService` writes to `admin_audit_logs` with `SecretSanitizer` applied to
  metadata. Every existing admin mutation already calls it.
- RLS is enabled on every `public` table with no anon/authenticated policies
  (`backend/.../Data/PostgresPublicRls.cs`), which blocks PostgREST entirely.
- CSRF uses a double-submit cookie (`banter_csrf` + `X-CSRF-Token`).
- `frontend/src/proxy.ts` guards `/admin` in the browser. It is UX-only.

## Analytics inventory

No product analytics existed before this workstream.

- No PostHog, Plausible, GA4, `@vercel/analytics` or any event pipeline.
- Google AdSense loads from `frontend/src/app/layout.tsx` whenever a client ID is set.
- No cookie/consent banner of any kind. The existing "terms acceptance" flow is a legal
  ToS gate for guest sessions, not analytics consent.
- First-party error reporting exists: `POST /api/errors/client` and the
  `OperationalError` / `ApplicationErrorLog` tables.

## Job inventory

Hangfire runs in-process on the API host. `backend/.../Integrations/Jobs/JobRegistry.cs`
defines 18 jobs behind stable keys, and `HangfireJobRegistration` wires the cron
schedules from the `BackgroundJobs` configuration section.

Key jobs: `score-sync`, `match-details-sync`, `standings-sync`, `news-ingest`,
`ai-reactions`, `rss.sync`, `youtube.search.sync`, `openai.opinion.extract`,
`football.*.sync`.

Execution history is persisted to `sync_runs` and `sync_errors` through
`SyncRunTracker`; failures also land in `errors` (`OperationalError`) with fingerprint
deduplication. Pause/enable state lives in `job_registry_state`.

Admin already has list, run, pause, resume, enable, disable, retry and run-history
endpoints, all audited and rate-limited.

Storage is `UseInMemoryStorage()`, so schedules and job state do not survive a restart.
Recorded in `TECH_DEBT.md`.

## Cache inventory

Effectively nothing shared.

- `Cache-Control: public, max-age=60` on the feed endpoint.
- In-process `ConcurrentDictionary` caches for GIF search and OpenFootball fixtures.
- TanStack Query `staleTime` of 30-60s on the client.
- `ContentSourceOptions.CacheDuration` is configured but never read.

No `IMemoryCache`, `IDistributedCache`, Redis, or Next.js `revalidateTag`/`use cache`.

## Database impact assessment

EF Core is the single source of truth for schema. Migrations live in
`backend/BanterApp.Api/Data/Migrations/` and are applied with
`scripts/run-migrations.ps1` (`dotnet ef database update`).

`supabase/migrations/*.sql` contains an older direct-to-Supabase design with a
`profiles` table and PostgREST RLS policies. It is divergent from the live schema and
must not be treated as authoritative.

`frontend/prisma/schema.prisma` is an empty stub with no models.

Existing tables relevant to this work: `users` (with `IsPlatformAdmin` and
`AccountStatus`), `anonymous_users`, `admin_audit_logs`, `auth_audit_logs`,
`sync_runs`, `errors`, `job_registry_state`.

## Frontend structure

- App Router only, under `frontend/src/app/`.
- `AppShell` provides the product chrome and hides itself on `/admin` routes.
- `/admin` has its own `AdminShell` with a zinc dark palette, sidebar nav from
  `admin-nav-items.ts`, `AdminToastProvider`, `ResponsiveDataTable`, `StatCard`,
  `ConfirmDialog` and `StatusBadge`.
- Data fetching is client-side React Query against `apiFetch` (`frontend/src/lib/api.ts`),
  which attaches the Supabase bearer token, `X-Anonymous-Id`, `X-CSRF-Token` and
  `X-Country-Code`. There are no server actions.
- Types are hand-written (`lib/types.ts`, `lib/admin/types.ts`); nothing is generated
  from Supabase.
- No form library (no react-hook-form, no zod). Forms use `useState` plus manual checks.
- No charting library is installed.

## Existing admin pages

`/admin`, `/admin/jobs`, `/admin/jobs/[jobKey]/runs`, `/admin/errors`, `/admin/sources`,
`/admin/source-items`, `/admin/review`, `/admin/stats`, `/admin/football-data`,
`/admin/health`, `/admin/launch-checklist`.

## Gap summary against the cursor kit

| Kit phase | Status |
| --- | --- |
| 0 Repository audit | Done (this document) |
| 1 Supabase Auth foundation | Mostly done; password reset and guest claim missing |
| 2 Admin authorization | Done; permission granularity added in this workstream |
| 3 Admin shell + user management | Shell done; user management missing |
| 4 Privacy and consent | Missing |
| 5 Product analytics | Missing |
| 6 Analytics admin pages | Missing |
| 7 Background job operations | Largely done |
| 8 Caching | Missing |
| 9 Cache admin operations | Missing |
| 10 Privacy/user rights operations | Missing |
| 11 Production hardening | Partial |
