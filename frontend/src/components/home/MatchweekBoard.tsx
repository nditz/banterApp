"use client";

import { MatchCard } from "@/components/prediction/MatchCard";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { useCurrentMatchweek } from "@/hooks/useMatches";
import { groupMatchesByUkDate } from "@/lib/matchweek";

export function MatchweekBoard() {
  const { data, isLoading, isError } = useCurrentMatchweek();
  const matches = data?.matches ?? [];
  const days = groupMatchesByUkDate(matches);

  return (
    <Panel
      title={data?.number ? `Matchweek ${data.number}` : "Current matchweek"}
      subtitle="Premier League 2026/27 · same rounds as BBC Sport"
      accent="pitch"
    >
      {isError && (
        <p className="mb-3 text-xs text-muted-foreground">Official 2026/27 fixtures shown</p>
      )}
      {isLoading ? (
        <div className="space-y-3">
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      ) : matches.length === 0 ? (
        <p className="text-sm text-muted-foreground">No fixtures in this matchweek yet.</p>
      ) : (
        <div className="space-y-5">
          {days.map((day) => (
            <section key={day.key} className="space-y-3">
              <h3 className="text-[11px] font-bold uppercase tracking-[0.14em] text-muted-foreground">
                {day.label}
              </h3>
              {day.matches.map((match) => (
                <MatchCard key={match.id} match={match} />
              ))}
            </section>
          ))}
        </div>
      )}
    </Panel>
  );
}
