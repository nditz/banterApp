-- BanterApp Phase 1: initial schema, indexes, RLS, and triggers
-- PostgreSQL via Supabase

-- ---------------------------------------------------------------------------
-- Extensions
-- ---------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ---------------------------------------------------------------------------
-- Custom types
-- ---------------------------------------------------------------------------
CREATE TYPE public.match_status AS ENUM ('scheduled', 'live', 'finished');

CREATE TYPE public.prediction_type AS ENUM ('result', 'score', 'double_chance');

CREATE TYPE public.generated_content_type AS ENUM (
  'banter',
  'analysis',
  'meme',
  'video_script'
);

-- ---------------------------------------------------------------------------
-- Helper: read anonymous_user_id from JWT custom claim (set by backend/API)
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.get_anonymous_user_id()
RETURNS uuid
LANGUAGE sql
STABLE
AS $$
  SELECT NULLIF(auth.jwt() ->> 'anonymous_user_id', '')::uuid;
$$;

-- ---------------------------------------------------------------------------
-- Helper: auto-update updated_at
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$;

-- ---------------------------------------------------------------------------
-- 1. profiles (extends auth.users)
-- ---------------------------------------------------------------------------
CREATE TABLE public.profiles (
  id uuid PRIMARY KEY REFERENCES auth.users (id) ON DELETE CASCADE,
  display_name text,
  avatar_url text,
  is_adult_verified boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TRIGGER profiles_set_updated_at
  BEFORE UPDATE ON public.profiles
  FOR EACH ROW
  EXECUTE FUNCTION public.set_updated_at();

-- Auto-create profile on signup
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  INSERT INTO public.profiles (id, display_name, avatar_url)
  VALUES (
    NEW.id,
    COALESCE(NEW.raw_user_meta_data ->> 'display_name', NEW.raw_user_meta_data ->> 'full_name', split_part(NEW.email, '@', 1)),
    NEW.raw_user_meta_data ->> 'avatar_url'
  );
  RETURN NEW;
END;
$$;

CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW
  EXECUTE FUNCTION public.handle_new_user();

-- ---------------------------------------------------------------------------
-- 2. anonymous_users
-- ---------------------------------------------------------------------------
CREATE TABLE public.anonymous_users (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  recovery_code text NOT NULL UNIQUE,
  cookie_id text NOT NULL UNIQUE,
  display_name text,
  ai_generations_used integer NOT NULL DEFAULT 0,
  created_at timestamptz NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------------
-- 3. matches
-- ---------------------------------------------------------------------------
CREATE TABLE public.matches (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  external_id text,
  team_a text NOT NULL,
  team_b text NOT NULL,
  team_a_code text,
  team_b_code text,
  kickoff_time timestamptz NOT NULL,
  status public.match_status NOT NULL DEFAULT 'scheduled',
  home_score integer,
  away_score integer,
  group_name text,
  stage text,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_matches_kickoff_time ON public.matches (kickoff_time);

CREATE TRIGGER matches_set_updated_at
  BEFORE UPDATE ON public.matches
  FOR EACH ROW
  EXECUTE FUNCTION public.set_updated_at();

-- ---------------------------------------------------------------------------
-- 4. predictions
-- ---------------------------------------------------------------------------
CREATE TABLE public.predictions (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid REFERENCES public.profiles (id) ON DELETE CASCADE,
  anonymous_user_id uuid REFERENCES public.anonymous_users (id) ON DELETE CASCADE,
  match_id uuid NOT NULL REFERENCES public.matches (id) ON DELETE CASCADE,
  prediction_type public.prediction_type NOT NULL,
  prediction_value jsonb NOT NULL,
  points_awarded integer NOT NULL DEFAULT 0,
  locked_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT predictions_exactly_one_actor CHECK (
    (user_id IS NOT NULL AND anonymous_user_id IS NULL)
    OR (user_id IS NULL AND anonymous_user_id IS NOT NULL)
  )
);

CREATE INDEX idx_predictions_match_id ON public.predictions (match_id);
CREATE INDEX idx_predictions_user_id ON public.predictions (user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_predictions_anonymous_user_id ON public.predictions (anonymous_user_id) WHERE anonymous_user_id IS NOT NULL;

CREATE TRIGGER predictions_set_updated_at
  BEFORE UPDATE ON public.predictions
  FOR EACH ROW
  EXECUTE FUNCTION public.set_updated_at();

-- ---------------------------------------------------------------------------
-- 5. leagues
-- ---------------------------------------------------------------------------
CREATE TABLE public.leagues (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name text NOT NULL,
  invite_code text NOT NULL UNIQUE,
  owner_id uuid NOT NULL REFERENCES public.profiles (id) ON DELETE CASCADE,
  is_public boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_leagues_invite_code ON public.leagues (invite_code);

-- ---------------------------------------------------------------------------
-- 6. league_members
-- ---------------------------------------------------------------------------
CREATE TABLE public.league_members (
  league_id uuid NOT NULL REFERENCES public.leagues (id) ON DELETE CASCADE,
  user_id uuid NOT NULL REFERENCES public.profiles (id) ON DELETE CASCADE,
  joined_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (league_id, user_id)
);

CREATE INDEX idx_league_members_user_id ON public.league_members (user_id);

-- ---------------------------------------------------------------------------
-- 7. pundits
-- ---------------------------------------------------------------------------
CREATE TABLE public.pundits (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name text NOT NULL,
  organization text,
  avatar_url text,
  created_at timestamptz NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------------
-- 8. pundit_predictions
-- ---------------------------------------------------------------------------
CREATE TABLE public.pundit_predictions (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  pundit_id uuid NOT NULL REFERENCES public.pundits (id) ON DELETE CASCADE,
  match_id uuid NOT NULL REFERENCES public.matches (id) ON DELETE CASCADE,
  prediction_type public.prediction_type NOT NULL,
  prediction_value jsonb NOT NULL,
  source_name text NOT NULL,
  source_url text,
  published_at timestamptz,
  author text
);

CREATE INDEX idx_pundit_predictions_match_id ON public.pundit_predictions (match_id);

-- ---------------------------------------------------------------------------
-- 9. generated_content (stub AI storage)
-- ---------------------------------------------------------------------------
CREATE TABLE public.generated_content (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid REFERENCES public.profiles (id) ON DELETE CASCADE,
  anonymous_user_id uuid REFERENCES public.anonymous_users (id) ON DELETE CASCADE,
  type public.generated_content_type NOT NULL,
  prompt text,
  output text,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT generated_content_exactly_one_actor CHECK (
    (user_id IS NOT NULL AND anonymous_user_id IS NULL)
    OR (user_id IS NULL AND anonymous_user_id IS NOT NULL)
  )
);

CREATE INDEX idx_generated_content_user_id ON public.generated_content (user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_generated_content_anonymous_user_id ON public.generated_content (anonymous_user_id) WHERE anonymous_user_id IS NOT NULL;

-- ---------------------------------------------------------------------------
-- 10. news_feed_items
-- ---------------------------------------------------------------------------
CREATE TABLE public.news_feed_items (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  source_name text NOT NULL,
  source_url text,
  title text NOT NULL,
  author text,
  published_at timestamptz,
  image_url text,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_news_feed_items_published_at ON public.news_feed_items (published_at DESC);

-- ---------------------------------------------------------------------------
-- 11. user_scores (aggregated leaderboards)
-- ---------------------------------------------------------------------------
CREATE TABLE public.user_scores (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid UNIQUE REFERENCES public.profiles (id) ON DELETE CASCADE,
  anonymous_user_id uuid UNIQUE REFERENCES public.anonymous_users (id) ON DELETE CASCADE,
  total_points integer NOT NULL DEFAULT 0,
  predictions_count integer NOT NULL DEFAULT 0,
  perfect_match_days integer NOT NULL DEFAULT 0,
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT user_scores_exactly_one_actor CHECK (
    (user_id IS NOT NULL AND anonymous_user_id IS NULL)
    OR (user_id IS NULL AND anonymous_user_id IS NOT NULL)
  )
);

CREATE TRIGGER user_scores_set_updated_at
  BEFORE UPDATE ON public.user_scores
  FOR EACH ROW
  EXECUTE FUNCTION public.set_updated_at();

-- ---------------------------------------------------------------------------
-- Row Level Security
-- ---------------------------------------------------------------------------
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.anonymous_users ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.matches ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.predictions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.leagues ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.league_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.pundits ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.pundit_predictions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.generated_content ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.news_feed_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_scores ENABLE ROW LEVEL SECURITY;

-- profiles: users read/update own
CREATE POLICY "profiles_select_own"
  ON public.profiles FOR SELECT
  USING (auth.uid() = id);

CREATE POLICY "profiles_update_own"
  ON public.profiles FOR UPDATE
  USING (auth.uid() = id)
  WITH CHECK (auth.uid() = id);

-- anonymous_users: no direct client access; backend uses service role
-- (RLS enabled with no policies = deny all for anon/authenticated roles)

-- matches: public read
CREATE POLICY "matches_select_public"
  ON public.matches FOR SELECT
  USING (true);

-- predictions: users CRUD own (registered via auth.uid, anonymous via JWT claim)
CREATE POLICY "predictions_select_own"
  ON public.predictions FOR SELECT
  USING (
    auth.uid() = user_id
    OR anonymous_user_id = public.get_anonymous_user_id()
  );

CREATE POLICY "predictions_insert_own"
  ON public.predictions FOR INSERT
  WITH CHECK (
    auth.uid() = user_id
    OR anonymous_user_id = public.get_anonymous_user_id()
  );

CREATE POLICY "predictions_update_own"
  ON public.predictions FOR UPDATE
  USING (
    auth.uid() = user_id
    OR anonymous_user_id = public.get_anonymous_user_id()
  )
  WITH CHECK (
    auth.uid() = user_id
    OR anonymous_user_id = public.get_anonymous_user_id()
  );

CREATE POLICY "predictions_delete_own"
  ON public.predictions FOR DELETE
  USING (
    auth.uid() = user_id
    OR anonymous_user_id = public.get_anonymous_user_id()
  );

-- leagues: members read; owner creates/updates; public leagues readable
CREATE POLICY "leagues_select_public"
  ON public.leagues FOR SELECT
  USING (is_public = true);

CREATE POLICY "leagues_select_member"
  ON public.leagues FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.league_members lm
      WHERE lm.league_id = leagues.id AND lm.user_id = auth.uid()
    )
  );

CREATE POLICY "leagues_select_owner"
  ON public.leagues FOR SELECT
  USING (owner_id = auth.uid());

CREATE POLICY "leagues_insert_owner"
  ON public.leagues FOR INSERT
  WITH CHECK (owner_id = auth.uid());

CREATE POLICY "leagues_update_owner"
  ON public.leagues FOR UPDATE
  USING (owner_id = auth.uid())
  WITH CHECK (owner_id = auth.uid());

CREATE POLICY "leagues_delete_owner"
  ON public.leagues FOR DELETE
  USING (owner_id = auth.uid());

-- league_members: members read league membership
CREATE POLICY "league_members_select_member"
  ON public.league_members FOR SELECT
  USING (
    user_id = auth.uid()
    OR EXISTS (
      SELECT 1 FROM public.league_members lm
      WHERE lm.league_id = league_members.league_id AND lm.user_id = auth.uid()
    )
  );

CREATE POLICY "league_members_insert_self"
  ON public.league_members FOR INSERT
  WITH CHECK (user_id = auth.uid());

CREATE POLICY "league_members_delete_self"
  ON public.league_members FOR DELETE
  USING (user_id = auth.uid());

-- pundits, pundit_predictions, news_feed_items: public read
CREATE POLICY "pundits_select_public"
  ON public.pundits FOR SELECT
  USING (true);

CREATE POLICY "pundit_predictions_select_public"
  ON public.pundit_predictions FOR SELECT
  USING (true);

CREATE POLICY "news_feed_items_select_public"
  ON public.news_feed_items FOR SELECT
  USING (true);

-- generated_content: users read own
CREATE POLICY "generated_content_select_own"
  ON public.generated_content FOR SELECT
  USING (
    auth.uid() = user_id
    OR anonymous_user_id = public.get_anonymous_user_id()
  );

-- user_scores: public read for leaderboards; users read own row
CREATE POLICY "user_scores_select_public"
  ON public.user_scores FOR SELECT
  USING (true);
