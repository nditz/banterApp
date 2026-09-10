"use client";

import { CalendarClock } from "lucide-react";
import { MatchCard } from "@/components/prediction/MatchCard";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useCurrentMatchweek } from "@/hooks/useMatches";
import { useStudio } from "@/hooks/useStudio";
import { FreshnessBadge } from "@/components/ui/freshness-badge";
import { isMatchLocked } from "@/lib/anonymous";
import { datasetStatusFromMatchweek, isUnofficialMatchweek } from "@/lib/football-dataset";
import { groupMatchesByUkDate } from "@/lib/matchweek";
import type { Match, StudioMatchComparison } from "@/lib/types";

export function MatchweekBoard() {
  const { data, isLoading, isError } = useCurrentMatchweek();
  const matches = data?.matches ?? [];
  const matchIds = matches.map((m) => m.id);
  const { data: comparison, isPending: comparisonLoading, isError: comparisonError, refetch: refetchComparison } =
    useStudio(matchIds);
  const comparisonById = new Map((comparison?.matches ?? []).map((row) => [row.matchId, row]));
  const comparisonStatus = comparisonLoading ? "loading" : comparisonError ? "error" : "ready";
  const days = groupMatchesByUkDate(matches);
  const status = datasetStatusFromMatchweek(data, isError);
  const subtitle = isUnofficialMatchweek(data)
    ? "Premier League 2026/27 · sample fixtures for local/dev"
    : "Premier League 2026/27 · same rounds as BBC Sport";
  const lockedCount = matches.filter((match) => isMatchLocked(match)).length;
  const progressLabel =
    matches.length > 0 ? `${lockedCount} of ${matches.length} locked` : undefined;

  const board = (
    <MatchweekDays
      days={days}
      comparisonById={comparisonById}
      filteringToFollows={comparison?.filteringToFollows}
      comparisonStatus={comparisonStatus}
      onRetryComparison={() => void refetchComparison()}
    />
  );

  return (
    <Panel
      title={data?.number ? `Matchweek ${data.number}` : "Current matchweek"}
      subtitle={subtitle}
      accent="pitch"
      action={
        progressLabel && !isLoading && status !== "error" ? (
          <p className="text-[11px] font-semibold tabular-nums text-muted-foreground">
            {progressLabel}
          </p>
        ) : undefined
      }
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
          <FreshnessBadge
            status="stale"
            label={
              data?.error ??
              "These kickoffs are overdue without results. Fixtures stay visible until score sync succeeds."
            }
          />
          {board}
        </div>
      ) : matches.length === 0 ? (
        <EmptyState
          dense
          icon={CalendarClock}
          title="No fixtures in this matchweek yet"
          description="Kickoffs appear here as soon as the schedule is published."
        />
      ) : (
        board
      )}
    </Panel>
  );
}

function MatchweekDays({
  days,
  comparisonById,
  filteringToFollows,
  comparisonStatus,
  onRetryComparison,
}: {
  days: ReturnType<typeof groupMatchesByUkDate>;
  comparisonById: Map<string, StudioMatchComparison>;
  filteringToFollows?: boolean;
  comparisonStatus: "loading" | "ready" | "error";
  onRetryComparison: () => void;
}) {
  return (
    <div className="space-y-5">
      {days.map((day) => (
        <section key={day.key} className="space-y-3">
          <h3 className="text-[11px] font-bold uppercase tracking-[0.14em] text-muted-foreground">
            {day.label}
          </h3>
          {day.matches.map((match: Match) => (
            <MatchCard
              key={match.id}
              match={match}
              comparison={comparisonById.get(match.id)}
              filteringToFollows={filteringToFollows}
              comparisonStatus={comparisonStatus}
              onRetryComparison={onRetryComparison}
            />
          ))}
        </section>
      ))}
    </div>
  );
}
