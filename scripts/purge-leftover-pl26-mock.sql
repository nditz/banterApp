-- One-off: remove leftover mock calendar rows (pl26-*) from Supabase.
-- Does NOT touch live football-data fixtures (fd-*).
--
-- Paste into Supabase Dashboard → SQL Editor and run as postgres.
-- First result sets are a preview; the transaction then deletes.
--
-- Why: leftover pl26-mw2-* (NS, late Aug) keep /api/health degraded.
-- pl26-mw1-5 stores Forest as NFO, so standings show 21 clubs (NFO + live NOT).
--
-- This is not run on API boot. After it commits, wait for the next score-sync
-- (~15 min) or restart the API so computed standings refresh.

begin;

drop table if exists pg_temp.tmp_pl26_matches;
create temporary table tmp_pl26_matches (
    "Id" text primary key
) on commit preserve rows;

insert into tmp_pl26_matches ("Id")
select m."Id"
from matches m
where m."Id" ilike 'pl26-%';

-- Preview: leftover fixtures
select
    m."Id",
    m."Status",
    m."KickoffTime",
    m."TeamA",
    m."TeamACode",
    m."TeamB",
    m."TeamBCode",
    m."MatchweekNumber"
from matches m
where m."Id" in (select "Id" from tmp_pl26_matches)
order by m."KickoffTime", m."Id";

-- Preview: user/anon predictions on those mock matches (will be deleted)
select
    p."Id",
    p."MatchId",
    p."UserId",
    p."AnonymousUserId",
    p."PredictionType",
    p."PredictionValue",
    p."PointsAwarded"
from predictions p
where p."MatchId" in (select "Id" from tmp_pl26_matches);

select
    'pl26_matches' as "Step",
    count(*) as "Count"
from tmp_pl26_matches
union all
select 'user_predictions_on_pl26', count(*)
from predictions p
where p."MatchId" in (select "Id" from tmp_pl26_matches)
union all
select 'pundit_predictions_on_pl26', count(*)
from pundit_predictions pp
where pp."MatchId" in (select "Id" from tmp_pl26_matches)
union all
select 'pundit_opinions_on_pl26', count(*)
from pundit_opinions o
where o."MatchId" in (select "Id" from tmp_pl26_matches)
union all
select 'news_feed_on_pl26', count(*)
from news_feed_items n
where n."MatchId" in (select "Id" from tmp_pl26_matches)
union all
select 'standing_rows_nfo', count(*)
from standing_rows s
where s."TeamCode" = 'NFO'
union all
select 'fd_matches_kept', count(*)
from matches m
where m."Id" ilike 'fd-%';

delete from news_feed_items n
where n."ParentItemId" in (
    select n2."Id"
    from news_feed_items n2
    where n2."MatchId" in (select "Id" from tmp_pl26_matches)
);

delete from news_feed_items n
where n."MatchId" in (select "Id" from tmp_pl26_matches);

delete from predictions p
where p."MatchId" in (select "Id" from tmp_pl26_matches);

delete from pundit_predictions pp
where pp."MatchId" in (select "Id" from tmp_pl26_matches);

delete from pundit_opinions o
where o."MatchId" in (select "Id" from tmp_pl26_matches);

delete from match_events e
where e."MatchId" in (select "Id" from tmp_pl26_matches);

delete from lineup_players lp
where lp."MatchId" in (select "Id" from tmp_pl26_matches);

delete from external_ids x
where x."EntityId" in (select "Id" from tmp_pl26_matches);

delete from banter_content_history b
where b."MatchId" in (select "Id" from tmp_pl26_matches);

delete from matches m
where m."Id" in (select "Id" from tmp_pl26_matches);

-- NFO only existed on mock Forest rows. Live Forest is NOT.
delete from standing_rows s
where s."TeamCode" = 'NFO';

select
    (select count(*) from matches where "Id" ilike 'pl26-%') as pl26_remaining,
    (select count(*) from matches where "Id" ilike 'fd-%') as fd_remaining,
    (select count(*) from matches) as matches_total,
    (select count(*) from standing_rows where "TeamCode" = 'NFO') as nfo_standing_rows,
    (select count(*) from standing_rows where "TeamCode" = 'NOT') as not_standing_rows;

commit;
