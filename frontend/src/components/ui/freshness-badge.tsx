import { cn } from "@/lib/utils";

export type FreshnessStatus = "ok" | "stale" | "live" | "unknown";

interface FreshnessBadgeProps {
  status: FreshnessStatus;
  label?: string;
  updatedAt?: string | number | Date | null;
  className?: string;
}

function formatUpdatedAt(value: string | number | Date): string | null {
  const time = value instanceof Date ? value.getTime() : new Date(value).getTime();
  if (!Number.isFinite(time)) return null;
  const mins = Math.max(0, Math.round((Date.now() - time) / 60_000));
  if (mins < 1) return "Updated just now";
  if (mins === 1) return "Updated 1 min ago";
  if (mins < 60) return `Updated ${mins} min ago`;
  const hours = Math.round(mins / 60);
  if (hours === 1) return "Updated 1 hour ago";
  if (hours < 24) return `Updated ${hours} hours ago`;
  return "Updated over a day ago";
}

const statusLabel: Record<FreshnessStatus, string> = {
  ok: "Live data",
  stale: "Cached — may be behind",
  live: "Live",
  unknown: "Time unknown",
};

/** Subtle freshness chrome. Stale data stays visible; this never replaces content. */
export function FreshnessBadge({
  status,
  label,
  updatedAt,
  className,
}: FreshnessBadgeProps) {
  const fromTime = updatedAt != null ? formatUpdatedAt(updatedAt) : null;
  const text = label ?? fromTime ?? statusLabel[status];

  return (
    <p
      role="status"
      className={cn(
        "text-xs leading-relaxed",
        status === "stale" || status === "unknown"
          ? "text-muted-foreground"
          : status === "live"
            ? "text-pitch"
            : "text-muted-foreground",
        className
      )}
    >
      {text}
    </p>
  );
}
