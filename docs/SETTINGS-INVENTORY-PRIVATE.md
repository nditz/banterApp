# Ball Takes — settings & integrations inventory (private)

**Gitignored.** This is the single index of every setting we run and every third-party
service we depend on. It is safe to record key *names*, where a value comes from and
whether it is currently set. **Do not paste actual secret values here** — they belong in
the platform's own secret store. If a value ever lands in this file, rotate it.

Companion files, both also gitignored:

- `docs/RENDER-ENV-PRIVATE.md` — exhaustive Render key list with examples and defaults
- `docs/GITHUB-DEPLOY-SETTINGS-PRIVATE.md` — GitHub/Vercel/Supabase deploy settings

Committed references: `backend/.env.template`, `frontend/.env.local.template`,
`docs/BACKEND-CONFIGURATION.md`, `docs/PRODUCTION-SECURITY.md`, `DEPLOYMENT.md`.

**Last reviewed:** 2026-08-25

---

## 1. How configuration resolves

**Backend (.NET).** `appsettings.json` → `appsettings.{Environment}.json` → environment
variables → command-line args. Later wins. On a host, `Section__Key` maps to
`Section:Key`, and arrays use `Section__Key__0`.

**Frontend (Next.js).** `NEXT_PUBLIC_*` is inlined into the browser bundle at build time
and is therefore public forever. Everything else stays server-side. Changing a
`NEXT_PUBLIC_*` value on Vercel requires a **redeploy**, not just a restart.

**Boot-time enforcement.** `backend/BanterApp.Api/Services/ProductionStartupValidator.cs`
refuses to start in Production unless the required set below is satisfied. That file is
the authority — if you add a new required setting, add it there too so a missing value
fails loudly at deploy instead of quietly at runtime.

---

## 2. Required in production — the service will not boot without these

Set on **Render**. Each maps to a `ProductionStartupValidator` check.

| Key | Source | Set? |
|-----|--------|------|
| `DATABASE_URL` *(or `ConnectionStrings__DefaultConnection`)* | Supabase → Settings → Database → Session pooler, port 5432 | ☐ |
| `Supabase__JwtSecret` | Supabase → Settings → API → JWT Settings | ☐ |
| `Security__SessionSecret` | Generate: `openssl rand -hex 32`. Must not be the dev default | ☐ |
| `Security__TurnstileSecretKey` | Cloudflare → Turnstile → site → Secret key | ☐ |
| `YouTube__ApiKey` | Google Cloud Console → YouTube Data API v3 | ☐ |
| `Ai__ApiKey` | OpenAI dashboard. Only required when `Ai__Provider` is `openai`/`chatgpt` | ☐ |
| `Legal__DisclaimerText` | Ours — see `RENDER-ENV-PRIVATE.md` for the exact one-liner | ☐ |
| `Legal__TermsUrl` | `https://balltakes.com/terms` | ☐ |
| `Legal__PrivacyPolicyUrl` | `https://balltakes.com/privacy` | ☐ |
| `ALLOWED_ORIGINS` | Comma-separated. Must contain no `localhost` | ☐ |
| `Admin__AllowedEmails__0` | First-admin bootstrap; can be dropped once a real `users.IsPlatformAdmin` row exists | ☐ |

Two further boot guards that are not settings but will kill startup: a production
connection string is mandatory, and it must not point at the Supabase direct endpoint
`db.<ref>.supabase.co` (IPv6-only, unreachable from Render). Use the pooler host.

---

## 3. Third-party integrations

One row per external dependency. "Degrades to" is what happens when the key is absent —
worth knowing before an outage rather than during one.

| Integration | Key(s) | Where the value comes from | Degrades to | Set? |
|---|---|---|---|---|
| **Supabase Auth** | `Supabase__Url`, `Supabase__AnonKey`, `Supabase__JwtSecret` | Dashboard → Settings → API | Hard failure — no sign-in | ☐ |
| **Supabase Admin API** | `Supabase__ServiceRoleKey` | Dashboard → Settings → API → `service_role` | `/admin/users` reports `identitySource: "database"`, hides sign-in history and providers | ☐ |
| **Supabase Postgres** | `DATABASE_URL` | Dashboard → Settings → Database → Session pooler (5432) | In-memory DB; nothing persists | ☐ |
| **Cloudflare Turnstile** | `Security__TurnstileSecretKey` + `NEXT_PUBLIC_TURNSTILE_SITE_KEY` | Cloudflare → Turnstile | Required in prod; locally the client returns `dev-bypass` | ☐ |
| **OpenAI** | `Ai__Provider`, `Ai__ApiKey`, `Ai__Model` | OpenAI dashboard | `Ai__Provider=stub` returns canned content | ☐ |
| **YouTube Data API v3** | `YouTube__ApiKey` | Google Cloud Console | Pundit/media YouTube ingest jobs no-op | ☐ |
| **API-Football** | `SportsData__Provider=apifootball`, `SportsData__ApiKey` | api-sports.io | `Provider=mock` serves fixture stubs | ☐ |
| **Sportmonks** *(fallback)* | `Sportmonks__Token`, `Sportmonks__LeagueId`, `Sportmonks__SeasonId` | sportmonks.com | Fallback chain skips this provider | ☐ |
| **football-data.org** *(fallback)* | `FootballData__Token` | football-data.org | Reference data sync skipped | ☐ |
| **OpenFootball** *(free JSON)* | `OpenFootball__Enabled`, `OpenFootball__JsonUrl` | Public GitHub, no key | Disabled | ☐ |
| **NewsAPI** *(optional)* | `News__ApiKey` | newsapi.org | RSS-only ingest, which is the normal path | ☐ |
| **Giphy** | `ReactionGif__ApiKey` | developers.giphy.com | Bundled static `/reactions` stickers | ☐ |
| **Google AdSense** | `NEXT_PUBLIC_ADSENSE_CLIENT` | AdSense dashboard | Falls back to the hardcoded publisher ID; gated on marketing consent either way | ☐ |
| **Google Search Console** | `NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION` | Search Console | Verification meta tag omitted | ☐ |
| **Vercel** | `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` | Vercel account settings | CI preview build and deploy-verify steps skip | ☐ |
| **Render** | `RENDER_API_KEY`, `RENDER_SERVICE_ID` | Render account settings | `deploy-verify` workflow can't query the service | ☐ |

RSS and podcast feeds need no credentials. Their URLs live in committed `appsettings.json`
under `News:RssFeedUrls`, `PunditIngest:RssFeedUrls` and `MediaIngest:*`, and outbound
fetches are constrained by `Security:AllowedFetchDomains` (SSRF allowlist).

---

## 4. Vercel — frontend

Set for Production, Preview and Development separately. Remember: a change needs a
redeploy to take effect.

| Key | Value | Public? | Set? |
|-----|-------|---------|------|
| `NEXT_PUBLIC_SUPABASE_URL` | `https://<ref>.supabase.co` | yes | ☐ |
| `NEXT_PUBLIC_SUPABASE_ANON_KEY` | anon key | yes, by design | ☐ |
| `NEXT_PUBLIC_SITE_URL` | `https://balltakes.com` | yes | ☐ |
| `NEXT_PUBLIC_TURNSTILE_SITE_KEY` | Turnstile site key | yes | ☐ |
| `API_PROXY_URL` | `https://api.balltakes.com` | no — server-only rewrite target | ☐ |
| `NEXT_PUBLIC_API_URL` | **leave empty** so the browser uses the same-origin `/api-backend` proxy | yes | ☐ |
| `NEXT_PUBLIC_APP_VERSION` | optional; `$VERCEL_GIT_COMMIT_SHA` stamps analytics events with a build | yes | ☐ |
| `NEXT_PUBLIC_ADSENSE_CLIENT` | optional override | yes | ☐ |
| `NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION` | optional | yes | ☐ |

**Never** put `Supabase__ServiceRoleKey`, `Supabase__JwtSecret`, `Security__SessionSecret`
or any `Ai__ApiKey` on Vercel. Nothing on Vercel needs them, and a `NEXT_PUBLIC_` prefix
on any of them would publish the value in the client bundle permanently.

Verification after a build:

```powershell
cd frontend
npm run build
Get-ChildItem -Recurse -File .next/static |
  Select-String -SimpleMatch 'service_role','serviceRoleKey','SERVICE_ROLE'
```

No output means clean.

---

## 5. GitHub — Actions

Repository → Settings → Secrets and variables → Actions, `production` environment.

| Name | Kind | Used by | Set? |
|------|------|---------|------|
| `DATABASE_URL` | secret | `migrate.yml` | ☐ |
| `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` | secret | `security-ci.yml`, `deploy-verify.yml` | ☐ |
| `RENDER_API_KEY`, `RENDER_SERVICE_ID` | secret | `deploy-verify.yml` | ☐ |
| `SUPABASE_URL`, `SUPABASE_ANON_KEY` | secret | `deploy-verify.yml` smoke tests | ☐ |
| `API_BASE_URL` | variable | defaults to `https://api.balltakes.com` | ☐ |
| `SITE_URL` | variable | defaults to `https://balltakes.com` | ☐ |
| `E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD` | secret | Playwright admin specs, if run in CI | ☐ |

---

## 6. Supabase dashboard — non-env settings

These are console toggles, not variables, and are easy to forget on a new project.

- **Authentication → Providers:** Email enabled; Google enabled with OAuth client ID and
  secret.
- **Authentication → URL Configuration:** Site URL `https://balltakes.com`; redirect URLs
  include `https://balltakes.com/**` and `http://localhost:3000/**` for local dev.
- **Settings → Database:** use the **session pooler on port 5432** for EF Core. The
  transaction pooler (6543) is recorded only as `Database__TransactionUrl` and is not what
  EF connects through.
- **Backups:** once enabled, set `Operations__BackupsConfigured=true` so the admin launch
  checklist reflects reality.

---

## 7. Consent version — the one value that lives in two places

`Privacy:ConsentVersion` on the backend and `CONSENT_VERSION` in
`frontend/src/lib/consent.ts` are compared on **every page load**.

- Backend: `appsettings.json` → `Privacy.ConsentVersion`, currently `2026-08-25`,
  overridable per environment with `Privacy__ConsentVersion`.
- Frontend: `export const CONSENT_VERSION = "2026-08-25";` — a code constant, deliberately
  not an env var so it can't drift per-environment inside a single build.

**Why it matters.** `readStoredConsent()` returns `null` when the stored record's version
differs from the constant, and `ConsentService.IsAnalyticsAllowedAsync` refuses a stored
grant whose version isn't current. That is the correct behaviour when the privacy notice
genuinely changes — a choice made against an older notice should be re-asked. But if the
two sides drift accidentally, every user is treated as undecided: the banner reappears on
every page load, analytics silently stops recording, and AdSense stops loading. It looks
like a bug, not a config error.

**When you change the privacy notice:**

1. Bump `CONSENT_VERSION` in `frontend/src/lib/consent.ts`.
2. Bump `Privacy.ConsentVersion` in `backend/BanterApp.Api/appsettings.json`.
3. If `Privacy__ConsentVersion` is set explicitly on Render, bump it there too — an
   environment override silently beats the committed default.
4. Ship both in the same commit and deploy them together. A frontend-only deploy makes
   every existing grant read as stale until the backend catches up.

Existing `consent_preferences` rows are kept, not deleted. They hold the older version
string and simply stop counting as a current grant, which preserves the audit trail of
what was agreed to and when.

The two kill switches are separate from versioning: `Privacy__AnalyticsEnabled=false` or
`Privacy__MarketingCategoryEnabled=false` deny that category for everyone immediately,
regardless of stored consent, and `ConsentService.SaveAsync` will refuse to record a grant
for a disabled category.

---

## 8. Database — Supabase cloud vs local Postgres

### How the backend picks a connection

`DatabaseConnection.Resolve` tries these in order and takes the first non-empty one:

1. `DATABASE_URL`
2. `ConnectionStrings:DefaultConnection`
3. `Database:DirectUrl`
4. `Database:TransactionUrl`

If none is set, the app falls back to an **in-memory** database — handy for tests,
silently lossy if you didn't intend it. The startup log line `Database provider: ...`
tells you which mode you're in; check it first when data mysteriously disappears.

> **Gotcha.** `DATABASE_URL` is read *before* `appsettings.Development.json`, so an exported
> shell variable silently overrides the local settings file and you keep hitting cloud
> Supabase while believing you switched. Check `echo $env:DATABASE_URL` before debugging
> anything else. Note the API does **not** read the repo-root `.env` — `DotNetEnv` is
> referenced in the csproj but never called (see `TECH_DEBT.md` T12), so only variables
> genuinely exported into the process environment take effect.

### URI vs ADO.NET form — both work everywhere

A value starting with `postgres` is parsed as a URI and rebuilt into an Npgsql connection
string. Any other value is passed through to Npgsql untouched. Use whichever you prefer;
the URI form is the one Supabase and Render hand you, so it's the path of least friction.

TLS on the URI path resolves in `DatabaseConnection.ResolveSslMode`:

| Connection string | Resulting `SslMode` |
|---|---|
| Explicit `?sslmode=…` | Exactly what you asked for (`disable`, `prefer`, `require`, `verify-ca`, `verify-full`) |
| Remote host, no `sslmode` | `Require` |
| `localhost` / `127.x` / `::1`, no `sslmode` | `Prefer` |

`Prefer` negotiates TLS when the server offers it and falls back to plaintext when it
doesn't, which is what a stock local Postgres needs. The loopback carve-out is deliberately
narrow — a remote host never silently downgrades, so a typo in a Supabase hostname can't
quietly send your password in the clear.

### Mode A — cloud Supabase (default)

`backend/BanterApp.Api/appsettings.Development.json` (gitignored, copy from
`.example`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://postgres.PROJECT_REF:URL_ENCODED_PASSWORD@aws-0-REGION.pooler.supabase.com:5432/postgres"
  }
}
```

URL-encode the password: `/` → `%2F`, `*` → `%2A`, `@` → `%40`. Use the **session pooler
on 5432**, not the transaction pooler and not `db.<ref>.supabase.co`.

### Mode B — local Postgres for data, cloud Supabase for auth (recommended for dev)

This is the useful hybrid: schema changes, seeding and destructive experiments stay on
your machine, while sign-in keeps working because JWTs are still issued by the cloud
project and validated locally against the same `Supabase:JwtSecret`.

```powershell
docker run --name banter-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=banterapp `
  -p 5433:5432 -d postgres:16
```

Then in `appsettings.Development.json`, keeping every `Supabase` key pointed at the cloud
project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://postgres:postgres@localhost:5433/banterapp"
  },
  "Database": { "TransactionUrl": "" }
}
```

The ADO.NET form works identically if you prefer it:
`Host=localhost;Port=5433;Database=banterapp;Username=postgres;Password=postgres`

Port 5433 avoids colliding with a Postgres you may already have on 5432. Clearing
`Database:TransactionUrl` is belt-and-braces: it sits last in the resolution order and is
only reached if `DefaultConnection` is empty or mistyped — which is exactly when you'd
least want a silent fall back to the cloud database.

Apply the schema — `DatabaseSeeder` runs `MigrateAsync` automatically at startup against
any relational provider, so `dotnet run` is usually enough. To do it explicitly:

```powershell
.\scripts\run-migrations.ps1
```

The frontend needs no change in this mode. It still talks to cloud Supabase for auth and
to your local API for data.

To go back to cloud, copy both database values from
`backend/BanterApp.Api/appsettings.Development.cloud.json` back into
`appsettings.Development.json`. That file holds only the two connection strings — every
other secret stays in one place — and is gitignored by the
`appsettings.Development.*.json` rule. Nothing else changes.

**Verified working.** All 23 EF migrations apply over the URI form above, producing 44
tables (43 entities plus `__EFMigrationsHistory`), and the API boots and seeds against it.
Note `dotnet run` takes its port from `launchSettings.json` and listens on **5000**;
`ASPNETCORE_URLS` set in the parent shell does not reach the child process.

### Mode C — fully local via the Supabase CLI

Gives you local Postgres, GoTrue and Studio, so you can work entirely offline.

```powershell
supabase init     # no supabase/config.toml exists yet, so this is a first-time step
supabase start
supabase status   # prints local URL, anon key, service_role key and JWT secret
```

Point every Supabase setting at the local stack — backend
`Supabase__Url=http://127.0.0.1:54321` plus the JWT secret, anon key and service-role key
from `supabase status`, and frontend `NEXT_PUBLIC_SUPABASE_URL` /
`NEXT_PUBLIC_SUPABASE_ANON_KEY` to match. Connection string:

```
Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres
```

> **Do not run `supabase db reset`.** `supabase/migrations/20240611000000_initial_schema.sql`
> is the abandoned original schema and has diverged completely from what EF Core builds: it
> defines 12 tables against EF's 43, and the ones it does define are different tables, not
> different spellings — `profiles` and `user_scores` have no counterpart in the EF model,
> which has `users` and `leaderboard_entries`. The CLI would apply it and leave you with a
> database the .NET API cannot read. EF Core migrations under
> `backend/BanterApp.Api/Data/Migrations/` are the only authority. Start the stack, then
> let the API migrate the database itself.

### Identifier casing — the one real trap

Worth knowing precisely, because it is half snake_case and half not.

`AppDbContext` sets **table** names explicitly with `ToTable("users")`, `ToTable("analytics_events")`
and so on, so every table is snake_case. **Column** names are not mapped at all, and the
project does not reference `EFCore.NamingConventions`, so EF falls back to the property
name and columns come out PascalCase: `"Id"`, `"EventName"`, `"OccurredAt"`.

Postgres folds unquoted identifiers to lower case, so a PascalCase column only resolves
when quoted. EF always quotes, so the API is fine — but hand-written SQL is not:

```sql
select eventname from analytics_events;    -- ERROR: column "eventname" does not exist
select "EventName" from analytics_events;  -- works
```

That asymmetry is the actual reason the legacy `supabase/migrations` SQL and the EF schema
can't be mixed, and why psql sessions and Supabase SQL-editor snippets need quoted column
names. Adding `UseSnakeCaseNamingConvention()` would make it uniform, but it would rewrite
every existing column and needs a migration — see `docs/balltakes-admin/TECH_DEBT.md`.

Google OAuth does not work against the local stack without extra provider configuration,
so Mode B is usually the better trade.

### Row-level security

`DatabaseSeeder` re-runs `PostgresPublicRls.EnableAllPublicTables` after every migration
on any Postgres target, local or cloud. It enables RLS with no anon policies on every
public table, so new tables are covered automatically and PostgREST cannot read them. No
action needed when adding a table — but don't assume RLS on a database you populated by
some other route.

---

## 9. Local development quick start

1. `copy backend\BanterApp.Api\appsettings.Development.json.example backend\BanterApp.Api\appsettings.Development.json`
   and fill in the connection string plus the `Supabase` block.
2. `copy frontend\.env.local.example frontend\.env.local` and fill in
   `NEXT_PUBLIC_SUPABASE_URL`, `NEXT_PUBLIC_SUPABASE_ANON_KEY`, leaving
   `NEXT_PUBLIC_API_URL` empty and `API_PROXY_URL=http://localhost:5000`.
3. `.\scripts\run-api.ps1` then `cd frontend && npm run dev`.
4. Put your email in `Admin:AllowedEmails` to bootstrap admin access, sign in once, then
   confirm `/admin` loads.

`Security__TurnstileSecretKey` can stay empty locally; the client sends `dev-bypass`
outside production. `Ai__Provider=stub` avoids spending OpenAI credit while developing.

---

## 10. Maintenance

Update this file whenever any of the following happens, in the same PR as the change:

- a new `*Options` class or config section is added to the backend
- a new `NEXT_PUBLIC_*` variable is introduced
- a new third-party service is integrated or retired
- `ProductionStartupValidator` gains or drops a required check
- the consent version is bumped

Then bump **Last reviewed** at the top.
