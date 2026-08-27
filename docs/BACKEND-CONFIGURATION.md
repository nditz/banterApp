# Backend configuration

The ASP.NET API uses the standard [.NET configuration hierarchy](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/). **Never commit secrets** to `appsettings.json`.

## Where settings live

| Layer | File / source | Committed? | Purpose |
|-------|----------------|------------|---------|
| Defaults | `backend/BanterApp.Api/appsettings.json` | Yes | Structure, non-secret defaults (intervals, URLs, mock providers) |
| Local secrets | `backend/BanterApp.Api/appsettings.Development.json` | **No** (gitignored) | Your Supabase DB URL, API keys, session secret |
| Template | `backend/BanterApp.Api/appsettings.Development.json.example` | Yes | Copy this to create your local file |
| Production | Host / GitHub Actions **environment secrets** | No | Same keys as Development, injected as env vars |

The **frontend** (Next.js) still uses repo-root `.env` / `frontend/.env.local` for `NEXT_PUBLIC_*` variables. Backend secrets do **not** belong in those files.

## Local setup

```powershell
cd C:\banterapp
copy backend\BanterApp.Api\appsettings.Development.json.example backend\BanterApp.Api\appsettings.Development.json
# Edit appsettings.Development.json — add ConnectionStrings:DefaultConnection and Supabase keys
.\scripts\run-migrations.ps1
.\scripts\run-api.ps1
```

### Session pooler (required for EF Core)

Set **`ConnectionStrings:DefaultConnection`** to the Supabase **session** pooler (port **5432**):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://postgres.PROJECT_REF:URL_ENCODED_PASSWORD@aws-0-eu-west-1.pooler.supabase.com:5432/postgres"
  }
}
```

URL-encode special password characters (`/` → `%2F`, `*` → `%2A`).

Optional **`Database:TransactionUrl`** (port 6543) is documented for reference; the API uses `DefaultConnection` for EF.

## Configuration keys (backend)

| Key | Example section | Used for |
|-----|-----------------|----------|
| `ConnectionStrings:DefaultConnection` | ConnectionStrings | PostgreSQL / Supabase (EF Core) |
| `Supabase:Url` | Supabase | JWT issuer validation |
| `Supabase:JwtSecret` | Supabase | Validate Supabase auth tokens |
| `Supabase:AnonKey` | Supabase | Auth API calls |
| `Supabase:ServiceRoleKey` | Supabase | Service-role operations (if added) |
| `Security:SessionSecret` | Security | Anonymous session / recovery tokens |
| `Security:TurnstileSecretKey` | Security | Bot checks (empty = disabled locally) |
| `SportsData:Provider` | SportsData | `mock` or `apifootball` |
| `SportsData:ApiKey` | SportsData | API-Football key |
| `FootballReferenceData:Provider` | FootballReferenceData | `api_sports`, `sportmonks`, or `googleapis` |
| `FootballReferenceData:CompetitionCode` | FootballReferenceData | e.g. `PL` |
| `FootballReferenceData:Season` | FootballReferenceData | e.g. `2026` |
| `FootballReferenceData:PredictionLockDeadline` | FootballReferenceData | ISO datetime when user predictions lock |
| `Sportmonks:Token` | Sportmonks | Sportmonks fallback/validation |
| `FootballData:Token` | FootballData | football-data.org fallback |
| `YouTube:ApiKey` | YouTube | YouTube Data API for pundit video discovery |
| `News:ApiKey` | News | NewsAPI.org key |
| `Ai:Provider` / `Ai:ApiKey` | Ai | LLM provider (Phase 2) |

### Media & news feeds (pro takes + main panel)

See **`docs/MEDIA-FEED-INTEGRATION.md`** for full setup (RSS, podcasts, YouTube, attribution).

| Key | Purpose |
|-----|---------|
| `News:RssFeedUrls` | RSS headlines for main feed (no API key required) |
| `NewsIngest:*` | Background job → `news_feed_items` (news + match desk) |
| `MediaIngest:PodcastSources` | Named podcast RSS → `media_items` |
| `MediaIngest:YouTubeChannels` | Named YouTube channels (needs `YouTube:ApiKey`) |
| `MediaIngest:WebsiteSources` | Article RSS for prediction extraction |
| `BackgroundJobs:NewsIngestIntervalMinutes` | How often news is pulled |
| `BackgroundJobs:MediaIngestIntervalMinutes` | How often podcasts/YouTube are scanned |

Non-secret tuning (job intervals, Premier League league id, etc.) stays in committed `appsettings.json`.

## Deploy / GitHub Actions

At deploy time, set **environment secrets** on your hosting platform. ASP.NET maps them to the same keys using double underscores:

| Secret name (GitHub / host) | Maps to |
|-----------------------------|---------|
| `ConnectionStrings__DefaultConnection` | Session pooler connection string |
| `Supabase__Url` | Supabase project URL |
| `Supabase__JwtSecret` | JWT signing secret |
| `Supabase__AnonKey` | Anon key |
| `SportsData__ApiKey` | API-Football |
| `Sportmonks__Token` | Sportmonks |
| `FootballData__Token` | football-data.org |
| `YouTube__ApiKey` | YouTube Data API |
| `News__ApiKey` | NewsAPI |
| `Security__SessionSecret` | Session HMAC secret |
| `Security__TurnstileSecretKey` | Turnstile |

Also set `ASPNETCORE_ENVIRONMENT=Production` (or `Staging`).

### Example GitHub Actions snippet

```yaml
env:
  ASPNETCORE_ENVIRONMENT: Production
  ConnectionStrings__DefaultConnection: ${{ secrets.DATABASE_URL }}
  Supabase__Url: ${{ secrets.SUPABASE_URL }}
  Supabase__JwtSecret: ${{ secrets.SUPABASE_JWT_SECRET }}
  Supabase__AnonKey: ${{ secrets.SUPABASE_ANON_KEY }}
  SportsData__Provider: apifootball
  SportsData__ApiKey: ${{ secrets.SPORTS_API_KEY }}
  News__ApiKey: ${{ secrets.NEWS_API_KEY }}
  Security__SessionSecret: ${{ secrets.SESSION_SECRET }}
```

Use a **GitHub Environment** (e.g. `production`) so secrets are scoped and require approval before deploy.

## Priority order

When the API starts, later sources override earlier ones:

1. `appsettings.json`
2. `appsettings.{Environment}.json` (e.g. Development — local secrets)
3. Environment variables / hosting secrets
4. Command-line arguments

So production secrets in GitHub **override** empty placeholders in `appsettings.json` without ever committing them.

## Migrations

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project backend/BanterApp.Api
```

Or: `.\scripts\run-migrations.ps1`

EF design-time tools read the same files via `AppDbContextFactory`.
