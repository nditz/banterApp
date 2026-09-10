"use client";

import { CalendarClock } from "lucide-react";
import { MatchCard } from "@/components/prediction/MatchCard";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useCurrentMatchweek } from "@/hooks/useMatches";
import { useStudio } from "@/hooks/useStudio";
import { datasetStatusFromMatchweek, isUnofficialMatchweek } from "@/lib/football-dataset";
import { groupMatchesByUkDate } from "@/lib/matchweek";

export function MatchweekBoard() {
  const { data, isLoading, isError } = useCurrentMatchweek();
  const matches = data?.matches ?? [];
  const matchIds = matches.map((m) => m.id);
  const { data: comparison } = useStudio(matchIds);
  const comparisonById = new Map((comparison?.matches ?? []).map((row) => [row.matchId, row]));
  const days = groupMatchesByUkDate(matches);
  const status = datasetStatusFromMatchweek(data, isError);
  const subtitle = isUnofficialMatchweek(data)
    ? "Premier League 2026/27 · sample fixtures for local/dev"
    : "Premier League 2026/27 · same rounds as BBC Sport";

  return (
    <Panel
      title={data?.number ? `Matchweek ${data.number}` : "Current matchweek"}
      subtitle={subtitle}
      accent="pitch"
    >
      {isLoading ? (
        <div className="space-y-3">
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      ) : status === "error" ? (
        <ErrorState
          dense
          title="Fixtures could not be loaded"
          description={
            data?.error ??
            "Current matchweek fixtures could not be loaded. This is not an empty week."
          }
        />
      ) : status === "stale" ? (
        <div className="space-y-5">
          <p role="status" className="text-sm text-muted-foreground">
            {data?.error ??
              "These kickoffs are overdue without results. Fixtures may be stale until score sync succeeds."}
          </p>
          {days.map((day) => (
            <section key={day.key} className="space-y-3">
              <h3 className="text-[11px] font-bold uppercase tracking-[0.14em] text-muted-foreground">
                {day.label}
              </h3>
              {day.matches.map((match) => (
                <MatchCard
                  key={match.id}
                  match={match}
                  comparison={comparisonById.get(match.id)}
                  filteringToFollows={comparison?.filteringToFollows}
                />
              ))}
            </section>
          ))}
        </div>
      ) : matches.length === 0 ? (
        <EmptyState
          dense
          icon={CalendarClock}
          title="No fixtures in this matchweek yet"
          description="Kickoffs appear here as soon as the schedule is published."
        />
      ) : (
        <div className="space-y-5">
          {days.map((day) => (
            <section key={day.key} className="space-y-3">
              <h3 className="text-[11px] font-bold uppercase tracking-[0.14em] text-muted-foreground">
                {day.label}
              </h3>
              {day.matches.map((match) => (
                <MatchCard
                  key={match.id}
                  match={match}
                  comparison={comparisonById.get(match.id)}
                  filteringToFollows={comparison?.filteringToFollows}
                />
              ))}
            </section>
          ))}
        </div>
      )}
    </Panel>
  );
}
