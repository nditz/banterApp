"use client";

import Link from "next/link";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { TeamFlag } from "@/components/brackets/TeamFlag";
import { useLeagueTable } from "@/hooks/useMatches";
import { cn } from "@/lib/utils";

type StandingRow = {
  rank: number;
  teamCode: string;
  teamName: string;
  logoUrl?: string;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalDiff: number;
  points: number;
};

export function LeagueTable({ compact = false }: { compact?: boolean }) {
  const { data, isLoading } = useLeagueTable();
  const rows = data ?? [];
  const showSplit = compact && rows.length > 9;
  const topRows = showSplit ? rows.slice(0, 6) : compact ? rows.slice(0, 8) : rows;
  const bottomRows = showSplit ? rows.slice(-3) : [];

  return (
    <Panel
      title={compact ? "Premier League table" : "Standings"}
      subtitle={compact ? "2026/27 · title race and drop" : "Premier League 2026/27"}
      accent="gold"
    >
      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : rows.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          Table appears once fixtures and results have synced.
        </p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[32rem] text-left text-sm">
            <thead className="text-[10px] font-bold uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="pb-2 pr-2">#</th>
                <th className="pb-2 pr-2">Club</th>
                <th className="pb-2 pr-2 text-right">P</th>
                <th className="pb-2 pr-2 text-right">W</th>
                <th className="pb-2 pr-2 text-right">D</th>
                <th className="pb-2 pr-2 text-right">L</th>
                <th className="pb-2 pr-2 text-right">GD</th>
                <th className="pb-2 text-right">Pts</th>
              </tr>
            </thead>
            <tbody>
              {topRows.map((row) => (
                <StandingsRow key={row.teamCode} row={row} />
              ))}
              {showSplit && (
                <tr>
                  <td
                    colSpan={8}
                    className="py-2 text-center text-[10px] uppercase tracking-wide text-muted-foreground"
                  >
                    ···
                  </td>
                </tr>
              )}
              {bottomRows.map((row) => (
                <StandingsRow key={row.teamCode} row={row} />
              ))}
            </tbody>
          </table>
        </div>
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

function StandingsRow({ row }: { row: StandingRow }) {
  const zone =
    row.rank <= 4
      ? "border-l-2 border-l-pitch/70"
      : row.rank >= 18
        ? "border-l-2 border-l-flare"
        : "border-l-2 border-l-transparent";

  return (
    <tr className={cn("border-t border-border", zone)}>
      <td className="py-2.5 pr-2 pl-2 font-mono text-xs text-muted-foreground">{row.rank}</td>
      <td className="py-2.5 pr-2">
        <span className="inline-flex min-w-0 items-center gap-2.5">
          <TeamFlag code={row.teamCode} name={row.teamName} logoUrl={row.logoUrl} size={24} />
          <span className="truncate font-medium">{row.teamName}</span>
        </span>
      </td>
      <td className="py-2.5 pr-2 text-right tabular-nums text-muted-foreground">{row.played}</td>
      <td className="py-2.5 pr-2 text-right tabular-nums">{row.won}</td>
      <td className="py-2.5 pr-2 text-right tabular-nums">{row.drawn}</td>
      <td className="py-2.5 pr-2 text-right tabular-nums">{row.lost}</td>
      <td className="py-2.5 pr-2 text-right tabular-nums">{row.goalDiff}</td>
      <td className="py-2.5 pr-2 text-right font-display text-base font-bold tabular-nums text-foreground">
        {row.points}
      </td>
    </tr>
  );
}
