"use client";

import Link from "next/link";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { TeamFlag } from "@/components/brackets/TeamFlag";
import { useLeagueTable } from "@/hooks/useMatches";
import { rankPremierLeagueTable, type LeagueTableRow } from "@/lib/league-table";
import { cn } from "@/lib/utils";

const compactGrid =
  "grid grid-cols-[2.25rem_minmax(0,1fr)_1.75rem_2.25rem_2.25rem] items-center gap-x-1";
const fullGrid =
  "grid grid-cols-[2.25rem_minmax(0,1fr)_1.75rem_2.25rem_2.25rem] items-center gap-x-1 md:grid-cols-[2.25rem_minmax(0,1.4fr)_repeat(6,1.75rem)_2.25rem_2.5rem]";

const headCell =
  "text-[10px] font-bold uppercase tracking-wide text-muted-foreground";
const numCell = "w-full text-right tabular-nums";

export function LeagueTable({ compact = false }: { compact?: boolean }) {
  const { data, isLoading } = useLeagueTable();
  const rows = rankPremierLeagueTable(data ?? []);
  const showSplit = compact && rows.length > 9;
  const topRows = showSplit ? rows.slice(0, 6) : compact ? rows.slice(0, 8) : rows;
  const bottomRows = showSplit ? rows.slice(-3) : [];
  const grid = compact ? compactGrid : fullGrid;

  return (
    <Panel
      title={compact ? "Premier League table" : "Standings"}
      subtitle={compact ? "2026/27 · title race and drop" : "Premier League 2026/27"}
      accent="gold"
      bodyClassName={cn("min-w-0", compact && "px-3 py-3")}
    >
      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : rows.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          Table appears once fixtures and results have synced.
        </p>
      ) : (
        <div className="min-w-0 w-full max-w-full" role="table" aria-label="Premier League table">
          <div className={cn(grid, "pb-2")} role="row">
            <span className={cn(headCell, "pl-2")} role="columnheader">
              #
            </span>
            <span className={headCell} role="columnheader">
              Club
            </span>
            <span className={cn(headCell, numCell)} role="columnheader">
              P
            </span>
            {!compact && (
              <>
                <span className={cn(headCell, numCell, "hidden md:block")} role="columnheader">
                  W
                </span>
                <span className={cn(headCell, numCell, "hidden md:block")} role="columnheader">
                  D
                </span>
                <span className={cn(headCell, numCell, "hidden md:block")} role="columnheader">
                  L
                </span>
                <span className={cn(headCell, numCell, "hidden md:block")} role="columnheader">
                  F
                </span>
                <span className={cn(headCell, numCell, "hidden md:block")} role="columnheader">
                  A
                </span>
              </>
            )}
            <span className={cn(headCell, numCell)} role="columnheader">
              GD
            </span>
            <span className={cn(headCell, numCell)} role="columnheader">
              Pts
            </span>
          </div>
          {topRows.map((row) => (
            <StandingsRow key={row.teamCode} row={row} compact={compact} grid={grid} />
          ))}
          {showSplit && (
            <p className="py-2 text-center text-[10px] uppercase tracking-wide text-muted-foreground">
              ···
            </p>
          )}
          {bottomRows.map((row) => (
            <StandingsRow key={row.teamCode} row={row} compact={compact} grid={grid} />
          ))}
        </div>
      )}
      {!compact && rows.length > 0 && (
        <p className="mt-3 text-[11px] leading-relaxed text-muted-foreground">
          Ranked by points, then goal difference, then goals scored — the same order used by
          the Premier League and BBC Sport.
        </p>
      )}
      {compact && (
        <p className="mt-3 border-t border-border pt-3 text-center">
          <Link href="/table" className="text-xs font-medium text-primary hover:underline">
            Full table →
          </Link>
        </p>
      )}
    </Panel>
  );
}

function StandingsRow({
  row,
  compact,
  grid,
}: {
  row: LeagueTableRow;
  compact: boolean;
  grid: string;
}) {
  const zone =
    row.rank <= 4 ? "bg-pitch/70" : row.rank >= 18 ? "bg-flare" : "bg-transparent";
  const gdLabel = row.goalDiff > 0 ? `+${row.goalDiff}` : String(row.goalDiff);

  return (
    <div className={cn(grid, "relative border-t border-border py-2")} role="row">
      <span
        className={cn("absolute top-1/2 left-0 h-3.5 w-0.5 -translate-y-1/2 rounded-full", zone)}
        aria-hidden
      />
      <span className="pl-2 font-mono text-xs tabular-nums text-muted-foreground" role="cell">
        {row.rank}
      </span>
      <span className="flex min-w-0 items-center gap-2" role="cell">
        <TeamFlag
          code={row.teamCode}
          name={row.teamName}
          logoUrl={row.logoUrl}
          size={compact ? 18 : 22}
        />
        <span className="truncate font-medium leading-tight">{row.teamName}</span>
      </span>
      <span className={cn(numCell, "text-muted-foreground")} role="cell">
        {row.played}
      </span>
      {!compact && (
        <>
          <span className={cn(numCell, "hidden md:block")} role="cell">
            {row.won}
          </span>
          <span className={cn(numCell, "hidden md:block")} role="cell">
            {row.drawn}
          </span>
          <span className={cn(numCell, "hidden md:block")} role="cell">
            {row.lost}
          </span>
          <span className={cn(numCell, "hidden md:block")} role="cell">
            {row.goalsFor}
          </span>
          <span className={cn(numCell, "hidden md:block")} role="cell">
            {row.goalsAgainst}
          </span>
        </>
      )}
      <span className={numCell} role="cell">
        {gdLabel}
      </span>
      <span className={cn(numCell, "font-bold text-foreground")} role="cell">
        {row.points}
      </span>
    </div>
  );
}
