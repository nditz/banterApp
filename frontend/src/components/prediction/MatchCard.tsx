"use client";

import { useMemo, useState } from "react";
import { Calendar, MapPin } from "lucide-react";
import { FixtureStatusBadge } from "@/components/prediction/FixtureStatusBadge";
import { PredictionButtons } from "@/components/prediction/PredictionButtons";
import { TeamFlag } from "@/components/brackets/TeamFlag";
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

function shortName(name: string): string {
  if (name.startsWith("Manchester")) return name.trim().split(/\s+/)[1] ?? name;
  if (name.startsWith("Brighton")) return "Brighton";
  if (name.startsWith("Tottenham")) return "Spurs";
  if (name.startsWith("Nottingham")) return "Forest";
  if (name.startsWith("Crystal")) return "Palace";
  if (name.startsWith("Aston")) return "Villa";
  if (name.startsWith("Newcastle")) return "Newcastle";
  if (name.startsWith("West ")) return name;
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return name;
  const last = parts[parts.length - 1];
  if (last === "United" || last === "City" || last === "Town") {
    return parts[0];
  }
  return last ?? name;
}

export function MatchCard({ match }: MatchCardProps) {
  const [selectedPrediction, setSelectedPrediction] = useState<string | null>(null);
  const { data: predictions } = usePredictionHistory();
  const matchPredictions = useMemo(
    () => predictions?.filter((p) => p.matchId === match.id) ?? [],
    [predictions, match.id]
  );
  const locked = isMatchLocked(match);
  const hasScore = match.homeScore != null && match.awayScore != null;
  const live = match.status === "LIVE" || match.status === "1H" || match.status === "2H" || match.status === "HT";

  return (
    <article className="match-card match-card-featured">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border px-3.5 py-2.5">
        <div className="flex flex-wrap items-center gap-2">
          {match.matchweekNumber ? (
            <span className="page-kicker">MW {match.matchweekNumber}</span>
          ) : null}
          <FixtureStatusBadge status={live ? "live" : locked ? "locked" : "open"} />
        </div>
        <div className="flex flex-wrap gap-x-3 gap-y-0.5 text-[11px] text-muted-foreground">
          <span className="inline-flex items-center gap-1">
            <Calendar className="size-3" aria-hidden />
            {formatKickoff(match.kickoffTime)}
          </span>
          {match.venue && (
            <span className="hidden items-center gap-1 sm:inline-flex">
              <MapPin className="size-3" aria-hidden />
              {match.venue}
            </span>
          )}
        </div>
      </div>

      <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-2 px-3 py-4 sm:gap-4 sm:px-4">
        <div className="flex min-w-0 flex-col items-end gap-1.5 text-right">
          {match.teamACode && (
            <TeamFlag
              code={match.teamACode}
              name={match.teamA}
              logoUrl={match.homeLogoUrl}
              size={40}
            />
          )}
          <p className="font-display text-sm leading-tight text-foreground sm:text-base">
            {shortName(match.teamA)}
          </p>
          {match.teamACode && (
            <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">
              {match.teamACode}
            </p>
          )}
        </div>

        <div className="flex min-w-[4.5rem] flex-col items-center justify-center">
          {hasScore ? (
            <p className="font-display text-3xl leading-none tabular-nums text-foreground sm:text-4xl">
              {match.homeScore}
              <span className="px-1 text-xl text-muted-foreground">–</span>
              {match.awayScore}
            </p>
          ) : (
            <p className="font-display text-xl text-muted-foreground sm:text-2xl">v</p>
          )}
          {live && <span className="live-chip mt-1.5">Live</span>}
        </div>

        <div className="flex min-w-0 flex-col items-start gap-1.5 text-left">
          {match.teamBCode && (
            <TeamFlag
              code={match.teamBCode}
              name={match.teamB}
              logoUrl={match.awayLogoUrl}
              size={40}
            />
          )}
          <p className="font-display text-sm leading-tight text-foreground sm:text-base">
            {shortName(match.teamB)}
          </p>
          {match.teamBCode && (
            <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">
              {match.teamBCode}
            </p>
          )}
        </div>
      </div>

      <div className="border-t border-border px-3.5 py-3">
        <PredictionButtons
          matchId={match.id}
          teamA={match.teamA}
          teamB={match.teamB}
          teamACode={match.teamACode}
          teamBCode={match.teamBCode}
          homeLogoUrl={match.homeLogoUrl}
          awayLogoUrl={match.awayLogoUrl}
          isLocked={locked}
          existingPredictions={matchPredictions}
          selectedValue={selectedPrediction}
          onSelect={setSelectedPrediction}
        />
      </div>
    </article>
  );
}
