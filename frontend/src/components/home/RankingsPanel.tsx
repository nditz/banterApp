"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { AdSlot } from "@/components/ads/AdSlot";
import { AuraSummary } from "@/components/rankings/AuraSummary";
import { LeaderboardTable } from "@/components/rankings/LeaderboardTable";
import { LeaderboardTabs } from "@/components/rankings/LeaderboardTabs";
import { LeagueSelector } from "@/components/rankings/LeagueSelector";
import { Panel } from "@/components/ui/panel";
import { ErrorState } from "@/components/ui/states";
import { buttonVariants } from "@/components/ui/button";
import {
  pickDefaultLeague,
  useLeagueLeaderboard,
  useMyLeagues,
} from "@/hooks/useLeaderboard";
import { cn } from "@/lib/utils";

export function RankingsPanel() {
  const { data: myLeagues, isLoading: leaguesLoading } = useMyLeagues();
  const leagues = useMemo(
    () => myLeagues?.leagues ?? [],
    [myLeagues?.leagues]
  );
  const limits = myLeagues?.limits;

  /** Explicit user pick from the league list; null until they choose. */
  const [userSelectedId, setUserSelectedId] = useState<string | null>(null);

  const selectedLeagueId = useMemo(() => {
    if (userSelectedId && leagues.some((l) => l.id === userSelectedId)) {
      return userSelectedId;
    }
    return pickDefaultLeague(leagues)?.id ?? null;
  }, [leagues, userSelectedId]);

  const selectedLeague = leagues.find((l) => l.id === selectedLeagueId) ?? null;

  const {
    data: standings,
    isLoading: standingsLoading,
    isError: standingsError,
  } = useLeagueLeaderboard(selectedLeagueId);

  return (
    <>
      <Panel
        id="rankings-heading"
        title="Aura rankings"
        subtitle={
          selectedLeague?.bonusPointsEnabled
            ? `${selectedLeague.name} · includes season-call points`
            : selectedLeague?.name ?? "Pick a league below"
        }
        accent="gold"
      >
        <div className="mb-3">
          <AuraSummary />
        </div>
        {standingsError ? (
          <ErrorState
            dense
            title="Couldn't load standings"
            description="Ranks are served live, so nothing is shown until we reach the server."
          />
        ) : (
          <LeaderboardTable
            entries={standings?.entries ?? []}
            me={standings?.me ?? null}
            totalPlayers={standings?.totalPlayers}
            isLoading={standingsLoading || leaguesLoading}
          />
        )}

        <div className="mt-4 border-t border-border pt-3">
          <LeaderboardTabs embedded punditsOnly />
        </div>
      </Panel>

      <Link
        href="/leagues"
        className={cn(
          buttonVariants({ variant: "outline", size: "sm" }),
          "mt-3 h-8 w-full text-xs"
        )}
      >
        Manage leagues
      </Link>

      <Panel title="My leagues" className="mt-3" accent="pitch">
        <LeagueSelector
          leagues={leagues}
          selectedId={selectedLeagueId}
          onSelect={setUserSelectedId}
          limits={limits}
        />
      </Panel>

      <AdSlot placement="sidebar" slotId="league-standings" className="mt-3" />
    </>
  );
}
