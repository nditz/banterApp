"use client";

import { useMemo, useState } from "react";
import { Calendar, MapPin } from "lucide-react";
import { FixtureStatusBadge } from "@/components/prediction/FixtureStatusBadge";
import { PredictionButtons } from "@/components/prediction/PredictionButtons";
import { Badge } from "@/components/ui/badge";
import { isMatchLocked } from "@/lib/anonymous";
import { usePredictionHistory } from "@/hooks/usePredictions";
import type { Match } from "@/lib/types";

interface MatchCardProps {
  match: Match;
}

function formatKickoff(iso: string): string {
  const date = new Date(iso);
  return new Intl.DateTimeFormat("en-GB", {
    weekday: "short",
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

export function MatchCard({ match }: MatchCardProps) {
  const [selectedPrediction, setSelectedPrediction] = useState<string | null>(
    null
  );
  const { data: predictions } = usePredictionHistory();
  const matchPredictions = useMemo(
    () => predictions?.filter((p) => p.matchId === match.id) ?? [],
    [predictions, match.id]
  );
  const locked = isMatchLocked(match);

  return (
    <article className="match-card match-card-featured overflow-hidden">
      <div className="border-b border-border/60 px-3.5 py-3">
        <div className="mb-2 flex flex-wrap items-center gap-2">
          {match.group && (
            <Badge
              variant="secondary"
              className="h-5 border-border/60 bg-muted/50 px-1.5 text-[10px] font-semibold uppercase tracking-wide"
            >
              {match.group}
            </Badge>
          )}
          <FixtureStatusBadge status={locked ? "locked" : "open"} />
        </div>
        <h3 className="font-display text-base font-semibold leading-snug">
          <span className="text-foreground">{match.teamA}</span>{" "}
          <span className="font-normal text-muted-foreground">v</span>{" "}
          <span className="text-foreground">{match.teamB}</span>
        </h3>
        <div className="mt-1.5 flex flex-wrap gap-x-3 gap-y-0.5 text-[11px] text-muted-foreground">
          <span className="inline-flex items-center gap-1">
            <Calendar className="size-3" aria-hidden />
            {formatKickoff(match.kickoffTime)}
          </span>
          {match.venue && (
            <span className="inline-flex items-center gap-1">
              <MapPin className="size-3" aria-hidden />
              {match.venue}
            </span>
          )}
        </div>
      </div>

      <div className="px-3.5 py-3">
        <PredictionButtons
          matchId={match.id}
          teamA={match.teamA}
          teamB={match.teamB}
          isLocked={locked}
          existingPredictions={matchPredictions}
          selectedValue={selectedPrediction}
          onSelect={setSelectedPrediction}
        />
      </div>
    </article>
  );
}
