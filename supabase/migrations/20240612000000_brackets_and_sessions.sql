-- Bracket picks + session terms (Phase 1 extension)

alter table if exists public.anonymous_users
  add column if not exists terms_accepted_at timestamptz;

alter table if exists public.profiles
  add column if not exists terms_accepted_at timestamptz;

alter table if exists public.predictions
  add column if not exists locked_at timestamptz;

create table if not exists public.bracket_picks (
  id uuid primary key default gen_random_uuid(),
  user_id uuid references public.profiles(id) on delete cascade,
  anonymous_user_id uuid references public.anonymous_users(id) on delete cascade,
  slot_id text not null,
  match_id text not null references public.matches(id) on delete cascade,
  winner_team_code text not null,
  locked_at timestamptz,
  created_at timestamptz not null default now(),
  check ((user_id is not null) <> (anonymous_user_id is not null))
);

create unique index if not exists bracket_picks_user_slot_idx
  on public.bracket_picks (user_id, slot_id)
  where user_id is not null;

create unique index if not exists bracket_picks_anon_slot_idx
  on public.bracket_picks (anonymous_user_id, slot_id)
  where anonymous_user_id is not null;

alter table public.bracket_picks enable row level security;
