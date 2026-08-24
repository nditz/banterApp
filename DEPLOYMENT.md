# Ball Takes — Full-Stack Deployment Guide

Production deployment for **balltakes.com** (frontend on Vercel) and **api.balltakes.com** (.NET 9 API on Render), with Supabase PostgreSQL and Cloudflare DNS.

## Architecture

```
Browser
  └─► Vercel (Next.js — balltakes.com)
        ├─► Supabase Auth (login/register/OAuth in browser)
        └─► /api-backend/* rewrite ──► Render API (api.balltakes.com)
              ├─► Supabase PostgreSQL (EF Core, session pooler :5432)
              ├─► Validates Supabase JWT (Supabase:JwtSecret)
              ├─► Cloudflare Turnstile (server secret)
              └─► OpenAI / YouTube / sports APIs (outbound)
```

The browser uses a **same-origin proxy** (`/api-backend` → Render). CORS on the API still applies for direct calls and credentials, but most traffic is proxied through Vercel.

---

## Prerequisites

- [ ] GitHub repository connected to Vercel and Render
- [ ] Supabase project with PostgreSQL (session pooler enabled)
- [ ] Cloudflare account managing `balltakes.com` DNS
- [ ] Cloudflare Turnstile site + secret keys
- [ ] OpenAI API key (if `Ai__Provider=openai`)
- [ ] YouTube Data API key

---

## 1. Render — Backend API

### Create Web Service

1. **Render Dashboard** → New → **Web Service**
2. Connect your GitHub repository
3. Configure:

| Setting | Value |
|---------|-------|
| **Name** | `balltakes-api` (or your choice) |
| **Root Directory** | `backend` |
| **Runtime** | Docker |
| **Dockerfile Path** | `Dockerfile` |
| **Health Check Path** | `/health` |
| **Instance Type** | Starter or higher (Hangfire background jobs need always-on) |

Render sets `PORT` automatically. The Dockerfile binds to `http://+:${PORT}`.

### Environment Variables

Copy from [`backend/.env.template`](backend/.env.template). **Required** for production startup:

| Variable | Example / Notes |
|----------|-----------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ALLOWED_ORIGINS` | `https://balltakes.com,https://www.balltakes.com` |
| `DATABASE_URL` | Supabase session pooler URI (port **5432**) |
| `Supabase__Url` | `https://xxx.supabase.co` |
| `Supabase__JwtSecret` | From Supabase → Settings → API → JWT Secret |
| `Supabase__AnonKey` | Supabase anon key |
| `Security__SessionSecret` | 32+ character random string |
| `Security__TurnstileSecretKey` | Cloudflare Turnstile secret |
| `Ai__Provider` | `openai` |
| `Ai__ApiKey` | OpenAI API key |
| `YouTube__ApiKey` | YouTube Data API key |
| `Admin__AllowedEmails__0` | Your admin email |
| `Legal__DisclaimerText` | Footer disclaimer copy |
| `Legal__TermsUrl` | `https://balltakes.com/terms` |
| `Legal__PrivacyPolicyUrl` | `https://balltakes.com/privacy` |

**Optional:** `GIT_COMMIT`, `BackgroundJobs__Enabled=true`, `SportsData__Provider=mock`

See also [`docs/BACKEND-CONFIGURATION.md`](docs/BACKEND-CONFIGURATION.md) and [`docs/PRODUCTION-SECURITY.md`](docs/PRODUCTION-SECURITY.md).

### Custom Domain

1. Render → your service → **Settings** → **Custom Domains**
2. Add `api.balltakes.com`
3. Render provides a CNAME target (e.g. `balltakes-api.onrender.com`)
4. Add the CNAME in Cloudflare (see DNS section below)
5. Wait for SSL certificate provisioning

### Database Migrations (first deploy)

After the first successful deploy, run EF migrations against production:

```bash
# From Render Shell, or locally with production DATABASE_URL:
dotnet ef database update --project backend/BanterApp.Api/BanterApp.Api.csproj
```

Or use your existing `scripts/run-migrations.ps1` with production connection string.

### Verify API

```bash
curl https://api.balltakes.com/health
# Expected: {"status":"ok"}

curl https://api.balltakes.com/api/health
# Expected: detailed JSON with database status
```

---

## 2. Vercel — Frontend

### Project Settings

| Setting | Value |
|---------|-------|
| **Root Directory** | `frontend` |
| **Framework Preset** | Next.js |
| **Build Command** | `npm run build` (default) |
| **Output Directory** | `.next` (default) |

### Environment Variables

Copy from [`frontend/.env.local.template`](frontend/.env.local.template):

| Variable | Production Value |
|----------|------------------|
| `API_PROXY_URL` | `https://api.balltakes.com` |
| `NEXT_PUBLIC_API_URL` | *(leave empty — uses proxy)* |
| `NEXT_PUBLIC_SITE_URL` | `https://balltakes.com` |
| `NEXT_PUBLIC_APP_URL` | `https://balltakes.com` |
| `NEXT_PUBLIC_SUPABASE_URL` | Your Supabase project URL |
| `NEXT_PUBLIC_SUPABASE_ANON_KEY` | Supabase anon key |
| `NEXT_PUBLIC_TURNSTILE_SITE_KEY` | Turnstile site key |

If Supabase is linked via Vercel integration, `NEXT_PUBLIC_SUPABASE_*` may be auto-provisioned.

### Custom Domains

1. Vercel → Project → **Settings** → **Domains**
2. Add `balltakes.com` and `www.balltakes.com`
3. Follow Vercel's DNS instructions for Cloudflare

---

## 3. Cloudflare DNS

**Important:** Disable Cloudflare proxy (grey cloud / DNS only) for all records below. Render and Vercel manage their own SSL.

| Type | Name | Target | Proxy |
|------|------|--------|-------|
| A | `@` (balltakes.com) | Vercel IP addresses (from Vercel domain settings) | Off |
| CNAME | `www` | `cname.vercel-dns.com` | Off |
| CNAME | `api` | `[your-service].onrender.com` | Off |

After DNS propagates (usually minutes, up to 48h):

- Frontend: `https://balltakes.com`
- API: `https://api.balltakes.com/health`

---

## 4. Supabase Configuration

### Auth Redirect URLs

Supabase Dashboard → **Authentication** → **URL Configuration**:

| Setting | Value |
|---------|-------|
| **Site URL** | `https://balltakes.com` |
| **Redirect URLs** | `https://balltakes.com/auth/callback` |
| | `https://www.balltakes.com/auth/callback` |
| | `http://localhost:3000/auth/callback` (local dev) |

### JWT Secret

Ensure `Supabase__JwtSecret` on Render matches **Settings → API → JWT Secret** in Supabase.

### Database Connection

Use the **session pooler** connection string (port **5432**) for EF Core:

```
postgresql://postgres.[PROJECT_REF]:[URL_ENCODED_PASSWORD]@aws-0-[REGION].pooler.supabase.com:5432/postgres
```

URL-encode special password characters (`/` → `%2F`, `*` → `%2A`).

---

## 5. Environment Variable Checklist

### Render (Backend) — copy before first deploy

```
ASPNETCORE_ENVIRONMENT=Production
ALLOWED_ORIGINS=https://balltakes.com,https://www.balltakes.com
DATABASE_URL=postgresql://...
Supabase__Url=https://xxx.supabase.co
Supabase__JwtSecret=...
Supabase__AnonKey=...
Security__SessionSecret=...
Security__TurnstileSecretKey=...
Ai__Provider=openai
Ai__ApiKey=...
YouTube__ApiKey=...
Admin__AllowedEmails__0=you@balltakes.com
Legal__DisclaimerText=...
Legal__TermsUrl=https://balltakes.com/terms
Legal__PrivacyPolicyUrl=https://balltakes.com/privacy
BackgroundJobs__Enabled=true
```

### Vercel (Frontend)

```
API_PROXY_URL=https://api.balltakes.com
NEXT_PUBLIC_API_URL=
NEXT_PUBLIC_SITE_URL=https://balltakes.com
NEXT_PUBLIC_APP_URL=https://balltakes.com
NEXT_PUBLIC_SUPABASE_URL=https://xxx.supabase.co
NEXT_PUBLIC_SUPABASE_ANON_KEY=...
NEXT_PUBLIC_TURNSTILE_SITE_KEY=...
```

---

## 6. Testing Checklist

After deploy, verify each item:

- [ ] `curl https://api.balltakes.com/health` returns `{"status":"ok"}`
- [ ] `curl https://api.balltakes.com/api/health` shows database connected
- [ ] `https://balltakes.com` loads the frontend
- [ ] Browser DevTools → Network: API calls go to `/api-backend/...` (not direct to api subdomain)
- [ ] User registration / login works (Supabase Auth)
- [ ] OAuth callback completes (`/auth/callback`)
- [ ] Predictions and feed load data from API
- [ ] Turnstile widget appears on protected forms
- [ ] Admin panel accessible for configured admin email
- [ ] CORS: no errors in console when using the app normally (proxy mode)

### CORS verification (optional direct test)

```bash
curl -H "Origin: https://balltakes.com" -I https://api.balltakes.com/health
# Should include: Access-Control-Allow-Origin: https://balltakes.com

curl -H "Origin: https://evil.com" -I https://api.balltakes.com/health
# Should NOT include Access-Control-Allow-Origin for evil.com
```

---

## 7. Troubleshooting

### API returns 502 on Render

- Check Render logs for startup validation errors
- Ensure all required env vars are set (see [`ProductionStartupValidator.cs`](backend/BanterApp.Api/Services/ProductionStartupValidator.cs))
- Verify `DATABASE_URL` is correct and Supabase allows connections from Render's IP

### Production startup validation failed

The API refuses to start if required secrets are missing. Read the error message — it lists each missing key. Common misses: `Legal__DisclaimerText`, `YouTube__ApiKey`, `Security__SessionSecret`.

### HTTPS redirect loop

Forwarded headers are configured in `Program.cs` for Render's reverse proxy. If issues persist, check that Cloudflare proxy is **disabled** (grey cloud) on the `api` CNAME.

### CORS errors in browser

With proxy mode (`NEXT_PUBLIC_API_URL` empty), browser calls `/api-backend` on the same origin — CORS should not apply. If you set `NEXT_PUBLIC_API_URL=https://api.balltakes.com` for direct calls, ensure `ALLOWED_ORIGINS` includes your frontend domain.

### JWT / auth failures

- Confirm `Supabase__JwtSecret` on Render matches Supabase dashboard
- Confirm `Supabase__Url` matches your project URL exactly
- Check Supabase redirect URLs include production domain

### Database connection errors

- Use session pooler port **5432**, not transaction pooler **6543**, for EF Core
- URL-encode password special characters
- Run migrations if tables don't exist

### Frontend can't reach API

- Verify `API_PROXY_URL=https://api.balltakes.com` in Vercel
- Redeploy frontend after changing env vars
- Check Vercel function logs for rewrite errors

---

## 8. Local Development

```powershell
# Backend
copy backend\BanterApp.Api\appsettings.Development.json.example backend\BanterApp.Api\appsettings.Development.json
# Edit with your Supabase keys
.\scripts\run-api.ps1

# Frontend
copy frontend\.env.local.template frontend\.env.local
# Edit for local values (API_PROXY_URL=http://localhost:5000)
cd frontend
npm run dev
```

Local URLs: frontend `http://localhost:3000`, API `http://localhost:5000`.

---

## 9. GitHub Actions

Vercel and Render already deploy when GitHub receives a push. Actions here **do not replace those deploys**. They catch migration drift, bad hosting settings, and a dead production URL.

| Workflow | When | What it does |
|----------|------|----------------|
| **Security CI** | Every push / PR | Backend tests, `dotnet ef migrations has-pending-model-changes`, Docker image build, lint/typecheck, static Vercel/Render/Supabase config checks |
| **Apply EF migrations** | `main` when backend data files change, or **Run workflow** | `dotnet ef database update` against the Supabase **session pooler** |
| **Deploy verify** | `main`, or **Run workflow** | Optional live inspect of Vercel/Render/Supabase settings, then curl `api.balltakes.com/health` and `balltakes.com` |

Create a GitHub **Environment** named `production` (Settings → Environments) and add secrets there. Required reviewers on that environment will gate migrations.

### Secrets (`production` environment)

| Secret | Used by | Notes |
|--------|---------|--------|
| `DATABASE_URL` | Apply EF migrations | Same session pooler URI as Render (port **5432**) |
| `VERCEL_TOKEN` | Deploy verify | Account token with project read access |
| `VERCEL_ORG_ID` | Deploy verify | Team/org id from Vercel project settings |
| `VERCEL_PROJECT_ID` | Deploy verify | Project id from Vercel project settings |
| `RENDER_API_KEY` | Deploy verify | Render Account Settings → API Keys |
| `RENDER_SERVICE_ID` | Deploy verify | Web service id (`srv-…`) |
| `SUPABASE_URL` | Deploy verify | `https://<ref>.supabase.co` |
| `SUPABASE_ANON_KEY` | Deploy verify | Anon/publishable key (not service role) |

If a live-inspect secret is missing, that provider is skipped instead of failing the job.

### Optional variables (repository or `production` environment)

| Variable | Default |
|----------|---------|
| `API_BASE_URL` | `https://api.balltakes.com` |
| `SITE_URL` | `https://balltakes.com` |

The API still runs `Database.MigrateAsync` on startup. The migrate workflow is the explicit failure path when the database cannot be updated, and it can be run before you rely on a new Render deploy.

---

## Related Documentation

- [`docs/BACKEND-CONFIGURATION.md`](docs/BACKEND-CONFIGURATION.md) — backend config keys
- [`docs/PRODUCTION-SECURITY.md`](docs/PRODUCTION-SECURITY.md) — security checklist
- [`backend/.env.template`](backend/.env.template) — Render env var template
- [`frontend/.env.local.template`](frontend/.env.local.template) — Vercel env var template
