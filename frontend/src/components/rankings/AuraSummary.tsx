"use client";

import { Flame, Sparkles, TrendingUp, Trophy } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useAura } from "@/hooks/useAura";
import { cn } from "@/lib/utils";

function formatCount(value: number): string {
  return new Intl.NumberFormat("en-GB").format(value);
}

function Stat({
  icon: Icon,
  label,
  value,
  accent,
}: {
  icon: typeof Sparkles;
  label: string;
  value: string;
  accent?: string;
}) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="flex items-center gap-1 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
        <Icon className="size-3" aria-hidden />
        {label}
      </span>
      <span className={cn("font-display text-base font-semibold tabular-nums", accent)}>
        {value}
      </span>
    </div>
  );
}

/**
 * Aura at a glance. Every figure comes from settled predictions on the server —
 * there is no separate client-side total.
 */
export function AuraSummary() {
  const { summary, isLoading, isError } = useAura();

  if (isLoading) {
    return <Skeleton className="h-16 w-full rounded-xl" />;
  }

  if (isError) {
    return (
      <ErrorState
        dense
        title="Aura unavailable"
        description="Settled points will show here when the request succeeds."
      />
    );
  }

  if (summary.settledPicks === 0 && summary.total === 0) {
    return (
      <EmptyState
        dense
        title="Aura lands when results settle"
        description="Lock picks first — this board stays empty until a matchweek pays out. It is not a zero score."
      />
    );
  }

  const accuracy =
    summary.settledPicks > 0
      ? Math.round((summary.correctPicks / summary.settledPicks) * 100)
      : null;

  return (
    <div className="grid grid-cols-2 gap-3 rounded-xl border border-border bg-muted/30 px-3 py-2.5 sm:grid-cols-4">
      <Stat
        icon={Sparkles}
        label="Aura"
        value={formatCount(summary.total)}
        accent="text-flare"
      />
      <Stat
        icon={TrendingUp}
        label="This week"
        value={`${summary.weeklyChange > 0 ? "+" : ""}${formatCount(summary.weeklyChange)}`}
        accent={summary.weeklyChange > 0 ? "text-pitch" : undefined}
      />
      <Stat
        icon={Flame}
        label="Streak"
        value={summary.streak > 0 ? `${summary.streak}` : "—"}
      />
      <Stat
        icon={Trophy}
        label={summary.percentile !== null ? "Percentile" : "Accuracy"}
        value={
          summary.percentile !== null
            ? `Top ${Math.max(1, 100 - summary.percentile)}%`
            : accuracy !== null
              ? `${accuracy}%`
              : "—"
        }
      />
    </div>
  );
}
