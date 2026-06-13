# BanterApp — World Cup Prediction Battle Platform

Phase 1 MVP: predictions, scoring, leagues, leaderboards, news feed, and **stubbed** AI content (no live LLM calls).

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

## Phase 1 scope

- Match predictions (result, correct score, double chance)
- Scoring (+3 / +7 / +2 + bonuses)
- Leagues with invite codes
- Leaderboards (global, league, pundits)
- Anonymous users (cookie + recovery code)
- Sports data integration (mock + API-Football ready)
- News feed with attribution
- AI endpoints return **canned/template** content (no OpenAI/Anthropic/Gemini)

## Phase 2 (deferred)

- Live AI provider integration
- Token/cost analysis
- Meme image generation, video scripts
