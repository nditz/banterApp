# BanterApp — Supabase Setup

Phase 1 database schema for the World Cup Prediction Battle Platform.

## Prerequisites

- [Supabase account](https://supabase.com)
- [Supabase CLI](https://supabase.com/docs/guides/cli) (optional, recommended)

```bash
npm install -g supabase
```

## 1. Create a Supabase project

1. Go to [supabase.com/dashboard](https://supabase.com/dashboard) and create a new project.
2. Note your **Project URL**, **anon key**, and **service role key**.
3. Copy `.env.example` to `.env` in the repo root and fill in the Supabase values.

## 2. Configure Auth providers

In **Authentication → Providers**:

| Provider | Action |
|----------|--------|
| Email | Enable (confirm email optional for dev) |
| Google | Enable and add OAuth client ID/secret |

In **Authentication → URL Configuration**, set site URL to `http://localhost:3000` for local dev.

## 3. Apply migrations

### Option A: Supabase CLI (recommended)

```bash
cd C:\banterapp
supabase login
supabase link --project-ref <your-project-ref>
supabase db push
```

### Option B: SQL Editor (dashboard)

1. Open **SQL Editor** in the Supabase dashboard.
2. Paste and run the contents of `migrations/20240611000000_initial_schema.sql`.
3. Paste and run `seed.sql` for sample data.

## 4. Load seed data

```bash
supabase db execute --file supabase/seed.sql
```

Or run `seed.sql` in the SQL Editor after migrations.

## Schema overview

| Table | Purpose |
|-------|---------|
| `profiles` | Registered user profile (extends `auth.users`) |
| `anonymous_users` | Cookie + recovery-code identities (backend-managed) |
| `matches` | Fixtures and results |
| `predictions` | User/anon match predictions |
| `leagues` | Private/public prediction leagues |
| `league_members` | League membership |
| `pundits` | Media analyst profiles |
| `pundit_predictions` | Attributed external predictions |
| `generated_content` | Stub AI output storage |
| `news_feed_items` | Sports news with attribution |
| `user_scores` | Aggregated leaderboard totals |

## Key design decisions

### UUID primary keys

All tables use `uuid` with `gen_random_uuid()` (via `pgcrypto`). Seed data uses fixed UUIDs for idempotent re-runs.

### Actor model (registered vs anonymous)

`predictions`, `generated_content`, and `user_scores` enforce **exactly one** of `user_id` or `anonymous_user_id` via `CHECK` constraints.

### Anonymous access pattern

- `anonymous_users` has RLS enabled with **no client policies** — creation and lookup use the **service role** from the ASP.NET API.
- Anonymous prediction/content access uses a custom JWT claim: `anonymous_user_id`.
- The backend signs short-lived tokens (or uses service role for server-side writes) with this claim so RLS policies in `get_anonymous_user_id()` resolve correctly.

### Profiles auto-provisioned

A trigger on `auth.users` inserts a `profiles` row on signup, pulling `display_name` and `avatar_url` from OAuth metadata when available.

### RLS (defense in depth)

| Table | Policy summary |
|-------|----------------|
| `profiles` | Read/update own row |
| `predictions` | CRUD own (by `auth.uid()` or `anonymous_user_id` claim) |
| `leagues` | Public leagues readable; members read; owner CRUD |
| `league_members` | Members read membership; self join/leave |
| `matches`, `pundits`, `pundit_predictions`, `news_feed_items` | Public read |
| `generated_content` | Read own |
| `user_scores` | Public read (leaderboards) |
| `anonymous_users` | Service role only |

Service role bypasses RLS — use only on the backend, never in the browser.

### Indexes

- `matches.kickoff_time` — upcoming fixtures queries
- `predictions.match_id` — per-match prediction lookups
- `leagues.invite_code` — join-by-code
- `news_feed_items.published_at` — feed ordering

### Enums

- `match_status`: `scheduled`, `live`, `finished`
- `prediction_type`: `result`, `score`, `double_chance`
- `generated_content_type`: `banter`, `analysis`, `meme`, `video_script`

## prediction_value JSONB examples

```json
// result
{"outcome": "home_win", "label": "Brazil to win"}

// score
{"home": 2, "away": 1}

// double_chance
{"selection": "home_or_draw"}
```

## Local Supabase (optional)

```bash
supabase init    # if not already initialized
supabase start   # local Postgres + Auth + Studio at http://localhost:54323
supabase db reset  # apply migrations + seed
```

## Troubleshooting

| Issue | Fix |
|-------|-----|
| RLS blocks anon predictions | Ensure API sets `anonymous_user_id` JWT claim or uses service role |
| Profile missing after signup | Check `on_auth_user_created` trigger exists |
| Seed conflicts | Seed uses `ON CONFLICT DO NOTHING` — safe to re-run |
| Google OAuth redirect error | Add `http://localhost:3000/**` to redirect URLs in Supabase Auth settings |

## Files

```
supabase/
├── migrations/
│   └── 20240611000000_initial_schema.sql
├── seed.sql
└── README.md
```
