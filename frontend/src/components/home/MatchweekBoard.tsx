"use client";

import { MatchCard } from "@/components/prediction/MatchCard";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { useCurrentMatchweek } from "@/hooks/useMatches";

export function MatchweekBoard() {
  const { data, isLoading, isError } = useCurrentMatchweek();
  const matches = data?.matches ?? [];

  return (
    <Panel
      title={data?.number ? `Matchweek ${data.number}` : "Current matchweek"}
      subtitle="Premier League fixtures — lock in before kickoff"
      accent="pitch"
    >
      {isError && (
        <p className="mb-3 text-xs text-muted-foreground">Demo fixtures shown</p>
      )}
      {isLoading ? (
        <div className="space-y-3">
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      ) : matches.length === 0 ? (
        <p className="text-sm text-muted-foreground">No fixtures in this matchweek yet.</p>
      ) : (
        <div className="space-y-3">
          {matches.map((match) => (
            <MatchCard key={match.id} match={match} />
          ))}
        </div>
      )}
    </Panel>
  );
}
