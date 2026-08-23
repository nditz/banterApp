# BanterApp — Premier League prediction game (Ball Takes)

Predict the Premier League. Beat your mates. Come back next matchweek.

Matchweek picks, private leagues, season awards, and a banter feed. Guest play stays; World Cup is gone.

## Stack

| Layer | Tech |
|-------|------|
| Frontend | Next.js, TypeScript, Tailwind, Shadcn UI, React Query |
| Backend | ASP.NET Core — feature-based Minimal API (`backend/BanterApp.Api`) |
| Database | Supabase PostgreSQL + Auth + RLS |
| Hosting | Vercel (frontend), Vercel Functions or Azure (API) |

## Project structure

```
banterapp/
├── frontend/          # Next.js app
├── backend/           # ASP.NET Core API (BanterApp.Api)
├── supabase/          # SQL migrations & RLS
├── requirement-spec.md
└── .env.example
```

## Quick start

### 1. Supabase & backend config

1. Create a project at [supabase.com](https://supabase.com)
2. Copy `backend/BanterApp.Api/appsettings.Development.json.example` → `appsettings.Development.json`
3. Fill in `ConnectionStrings:DefaultConnection` (session pooler, port 5432) and Supabase keys  
   See **`docs/BACKEND-CONFIGURATION.md`** for all keys and GitHub deploy secrets.
4. Apply EF migrations: `.\scripts\run-migrations.ps1`

Frontend public vars: copy `.env.example` → `.env` / `frontend/.env.local` (Supabase URL + anon key only).

### 2. Backend

From the **project folder** (not `backend/` alone):

```powershell
cd backend/BanterApp.Api
dotnet run
```

Or from the repo root:

```powershell
.\scripts\run-api.ps1
```

API: `http://localhost:5000` — Swagger at `/swagger`

**Troubleshooting**

| Error | Fix |
|-------|-----|
| `Couldn't find a project to run` | You are in `backend/` — use `backend/BanterApp.Api` or `scripts/run-api.ps1` |
| `did not find dotnet.dll` at `sdk\10.0.101` | .NET SDK install is corrupted — reinstall: [Download .NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or `winget install Microsoft.DotNet.SDK.10` |
| `framework Microsoft.NETCore.App, version 9.0.0` | Install [.NET 9 runtime](https://dotnet.microsoft.com/download/dotnet/9.0), or install the SDK (includes runtime). SDK 10 can build this project via `global.json`. |

After the API starts, restart the frontend (`npm run dev`) so `/api-backend` proxy picks up session routes.

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

App: `http://localhost:3000`

## Current product

- Premier League 2026/27 matchweek predictions (result +3 / exact +7 / double chance +2)
- Perfect matchweek bonus (+5)
- Private leagues ranked across the season
- Season awards (title, top four, relegation, Golden Boot, etc.)
- Anonymous users (cookie + recovery code) with optional account claim
- Sports data: API-Football league `39`, season `2026` (mock provider for local)

## Deferred

- Pundit leaderboard / Me vs Pundits, receipts, Stripe, premium AI, other leagues
