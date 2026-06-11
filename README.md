# BanterApp — World Cup Prediction Battle Platform

Phase 1 MVP: predictions, scoring, leagues, leaderboards, news feed, and **stubbed** AI content (no live LLM calls).

## Stack

| Layer | Tech |
|-------|------|
| Frontend | Next.js, TypeScript, Tailwind, Shadcn UI, React Query |
| Backend | ASP.NET Core 9 — feature-based Minimal API |
| Database | Supabase PostgreSQL + Auth + RLS |
| Hosting | Vercel (frontend), Vercel Functions or Azure (API) |

## Project structure

```
banterapp/
├── frontend/          # Next.js app
├── backend/           # ASP.NET Core 9 API
├── supabase/          # SQL migrations & RLS
├── requirement-spec.md
└── .env.example
```

## Quick start

### 1. Supabase

1. Create a project at [supabase.com](https://supabase.com)
2. Run migrations: `supabase db push` (or apply SQL in `supabase/migrations/`)
3. Copy keys to `.env`

### 2. Backend

```bash
cd backend/BanterApp.Api
dotnet restore
dotnet run
```

API: `http://localhost:5000` — Swagger at `/swagger`

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
