"use client";

import { LeagueAvatar } from "@/components/ui/UserAvatar";
import type { League, LeagueLimits } from "@/lib/types";
import { cn } from "@/lib/utils";

interface LeagueSelectorProps {
  leagues: League[];
  selectedId: string | null;
  onSelect: (leagueId: string) => void;
  limits?: LeagueLimits;
}

const kindBadge: Record<string, { label: string; className: string }> = {
  global: { label: "Global", className: "bg-pitch/15 text-pitch" },
  country: { label: "Country", className: "bg-blue-500/15 text-blue-700" },
  custom: { label: "Custom", className: "bg-gold/15 text-amber-800" },
};

export function LeagueSelector({
  leagues,
  selectedId,
  onSelect,
  limits,
}: LeagueSelectorProps) {
  if (leagues.length === 0) {
    return (
        <p className="py-3 text-center text-xs text-muted-foreground">
        Global and country leagues load automatically once you&apos;re playing.
      </p>
    );
  }

  return (
    <div className="space-y-2">
      {limits && (
        <p className="text-[10px] text-muted-foreground">
          {limits.customLeaguesUsed}/{limits.customLeaguesMax} custom ·{" "}
          {limits.totalLeaguesUsed}/{limits.totalLeaguesMax} total (incl. Global & Country)
        </p>
      )}
      <ul className="space-y-1.5" role="listbox" aria-label="Your leagues">
        {leagues.map((league) => {
          const selected = league.id === selectedId;
          const badge = kindBadge[league.kind ?? "custom"] ?? kindBadge.custom;

          return (
            <li key={league.id}>
              <button
                type="button"
                role="option"
                aria-selected={selected}
                onClick={() => onSelect(league.id)}
                className={cn(
                  "flex w-full items-center gap-2.5 rounded-lg border px-3 py-2.5 text-left transition-colors",
                  selected
                    ? "border-gold/50 bg-gold/10 ring-1 ring-gold/30"
                    : "border-border bg-card hover:border-gold/30 hover:bg-muted/40"
                )}
              >
                <LeagueAvatar league={league} size={32} selected={selected} />
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-xs font-semibold">{league.name}</span>
                  <span className="mt-0.5 flex flex-wrap items-center gap-1.5 text-[10px] text-muted-foreground">
                    <span
                      className={cn(
                        "rounded-full px-1.5 py-0.5 font-medium",
                        badge.className
                      )}
                    >
                      {badge.label}
                    </span>
                    {league.memberCount > 0 && (
                      <span>
                        {league.memberCount}
                        {league.maxMembers ? ` / ${league.maxMembers}` : ""} players
                      </span>
                    )}
                    {league.myDisplayName && (
                      <span>· You as {league.myDisplayName}</span>
                    )}
                  </span>
                </span>
              </button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
