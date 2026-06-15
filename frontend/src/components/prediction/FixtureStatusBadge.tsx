import { Clock, Lock, Radio } from "lucide-react";
import { cn } from "@/lib/utils";

type FixtureStatus = "open" | "live" | "locked";

interface FixtureStatusBadgeProps {
  status: FixtureStatus;
  className?: string;
}

const config: Record<
  FixtureStatus,
  { label: string; icon: typeof Clock; className: string }
> = {
  open: {
    label: "Open for picks",
    icon: Clock,
    className: "border-pitch/40 bg-pitch/15 text-pitch",
  },
  live: {
    label: "Live",
    icon: Radio,
    className: "border-flare/40 bg-flare/15 text-flare",
  },
  locked: {
    label: "Locked in",
    icon: Lock,
    className: "border-muted-foreground/30 bg-muted/60 text-muted-foreground",
  },
};

export function FixtureStatusBadge({ status, className }: FixtureStatusBadgeProps) {
  const { label, icon: Icon, className: statusClass } = config[status];

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide",
        statusClass,
        className
      )}
    >
      {status === "live" && (
        <span className="live-chip-dot bg-flare" aria-hidden />
      )}
      <Icon className="size-3" aria-hidden />
      {label}
    </span>
  );
}
