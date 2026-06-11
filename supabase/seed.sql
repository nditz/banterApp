-- BanterApp Phase 1 seed data: World Cup fixtures, pundits, news
-- Run after migrations. Safe to re-run (uses fixed UUIDs + ON CONFLICT).

-- ---------------------------------------------------------------------------
-- Matches (6 World Cup style fixtures)
-- ---------------------------------------------------------------------------
INSERT INTO public.matches (
  id, external_id, team_a, team_b, team_a_code, team_b_code,
  kickoff_time, status, home_score, away_score, group_name, stage
) VALUES
  (
    'a1000001-0000-4000-8000-000000000001',
    'wc2026-gA-01',
    'Brazil', 'Serbia',
    'BRA', 'SRB',
    '2026-06-15 19:00:00+00', 'scheduled', NULL, NULL,
    'Group A', 'Group Stage'
  ),
  (
    'a1000001-0000-4000-8000-000000000002',
    'wc2026-gA-02',
    'Argentina', 'Morocco',
    'ARG', 'MAR',
    '2026-06-16 16:00:00+00', 'scheduled', NULL, NULL,
    'Group A', 'Group Stage'
  ),
  (
    'a1000001-0000-4000-8000-000000000003',
    'wc2026-gB-01',
    'France', 'Australia',
    'FRA', 'AUS',
    '2026-06-17 19:00:00+00', 'scheduled', NULL, NULL,
    'Group B', 'Group Stage'
  ),
  (
    'a1000001-0000-4000-8000-000000000004',
    'wc2026-gB-02',
    'England', 'USA',
    'ENG', 'USA',
    '2026-06-18 13:00:00+00', 'scheduled', NULL, NULL,
    'Group B', 'Group Stage'
  ),
  (
    'a1000001-0000-4000-8000-000000000005',
    'wc2026-gC-01',
    'Spain', 'Japan',
    'ESP', 'JPN',
    '2026-06-19 16:00:00+00', 'scheduled', NULL, NULL,
    'Group C', 'Group Stage'
  ),
  (
    'a1000001-0000-4000-8000-000000000006',
    'wc2026-qf-01',
    'Germany', 'Portugal',
    'GER', 'POR',
    '2026-07-10 20:00:00+00', 'scheduled', NULL, NULL,
    NULL, 'Quarter Final'
  )
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Pundits
-- ---------------------------------------------------------------------------
INSERT INTO public.pundits (id, name, organization, avatar_url) VALUES
  (
    'b2000001-0000-4000-8000-000000000001',
    'Alex Rivera',
    'ESPN FC',
    NULL
  ),
  (
    'b2000001-0000-4000-8000-000000000002',
    'Sam Okafor',
    'BBC Sport',
    NULL
  )
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Pundit predictions (with attribution)
-- ---------------------------------------------------------------------------
INSERT INTO public.pundit_predictions (
  id, pundit_id, match_id, prediction_type, prediction_value,
  source_name, source_url, published_at, author
) VALUES
  (
    'c3000001-0000-4000-8000-000000000001',
    'b2000001-0000-4000-8000-000000000001',
    'a1000001-0000-4000-8000-000000000001',
    'result',
    '{"outcome": "home_win", "label": "Brazil to win"}'::jsonb,
    'ESPN FC',
    'https://www.espn.com/soccer/story/_/id/example-brazil-serbia',
    '2026-06-10 09:00:00+00',
    'Alex Rivera'
  ),
  (
    'c3000001-0000-4000-8000-000000000002',
    'b2000001-0000-4000-8000-000000000001',
    'a1000001-0000-4000-8000-000000000002',
    'score',
    '{"home": 2, "away": 1, "label": "Argentina 2-1 Morocco"}'::jsonb,
    'ESPN FC',
    'https://www.espn.com/soccer/story/_/id/example-argentina-morocco',
    '2026-06-11 11:30:00+00',
    'Alex Rivera'
  ),
  (
    'c3000001-0000-4000-8000-000000000003',
    'b2000001-0000-4000-8000-000000000002',
    'a1000001-0000-4000-8000-000000000003',
    'double_chance',
    '{"selection": "home_or_draw", "label": "France or Draw"}'::jsonb,
    'BBC Sport',
    'https://www.bbc.com/sport/football/articles/example-france-australia',
    '2026-06-12 08:00:00+00',
    'Sam Okafor'
  ),
  (
    'c3000001-0000-4000-8000-000000000004',
    'b2000001-0000-4000-8000-000000000002',
    'a1000001-0000-4000-8000-000000000004',
    'result',
    '{"outcome": "draw", "label": "England vs USA to end in a draw"}'::jsonb,
    'BBC Sport',
    'https://www.bbc.com/sport/football/articles/example-england-usa',
    '2026-06-13 14:00:00+00',
    'Sam Okafor'
  )
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- News feed items
-- ---------------------------------------------------------------------------
INSERT INTO public.news_feed_items (
  id, source_name, source_url, title, author, published_at, image_url
) VALUES
  (
    'd4000001-0000-4000-8000-000000000001',
    'FIFA.com',
    'https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026/articles/example-draw-reaction',
    'World Cup 2026 draw sets up blockbuster Group A clash between Brazil and Serbia',
    'FIFA Editorial',
    '2026-06-01 12:00:00+00',
    NULL
  ),
  (
    'd4000001-0000-4000-8000-000000000002',
    'The Athletic',
    'https://theathletic.com/example-morocco-argentina-preview',
    'Morocco aim to repeat 2022 magic against Argentina in opener',
    'James Horncastle',
    '2026-06-14 07:30:00+00',
    NULL
  )
ON CONFLICT (id) DO NOTHING;
