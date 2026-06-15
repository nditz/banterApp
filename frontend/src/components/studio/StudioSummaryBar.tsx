import { Flame, Trophy, Users } from "lucide-react";
import type { StudioComparison } from "@/lib/types";

interface StudioSummaryBarProps {
  data: StudioComparison;
}

export function StudioSummaryBar({ data }: StudioSummaryBarProps) {
  const totalPicks = data.matches.reduce(
    (sum, m) => sum + m.picks.filter((p) => p.role === "me").length,
    0
  );

  return (
    <div className="flex flex-wrap gap-3">
      <StatCard
        icon={<Flame className="size-4 text-flare" />}
        label="Your points"
        value={String(data.myTotalPoints)}
      />
      <StatCard
        icon={<Trophy className="size-4 text-gold" />}
        label="League rank"
        value={
          data.myLeagueRank && data.leagueTotal
            ? `#${data.myLeagueRank} of ${data.leagueTotal}`
            : "—"
        }
      />
      <StatCard
        icon={<Users className="size-4 text-pitch" />}
        label="Active picks"
        value={`${totalPicks} match${totalPicks === 1 ? "" : "es"}`}
      />
    </div>
  );
}

function StatCard({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
}) {
  return (
    <div className="flex min-w-[130px] flex-1 items-center gap-3 rounded-xl border border-border bg-card px-4 py-3 shadow-sm">
      <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-muted">
        {icon}
      </span>
      <div className="min-w-0">
        <p className="text-[11px] text-muted-foreground">{label}</p>
        <p className="truncate text-base font-bold">{value}</p>
      </div>
    </div>
  );
}
