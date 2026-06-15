import { cn } from "@/lib/utils";
import type { StudioPickRole } from "@/lib/types";

interface PickChipProps {
  prediction: string;
  role: StudioPickRole;
  pointsAwarded?: number;
  /** If true the chip is slightly muted (not "You") */
  secondary?: boolean;
}

const roleColors: Record<StudioPickRole, string> = {
  me: "bg-pitch/15 text-pitch border-pitch/30",
  league: "bg-blue-500/10 text-blue-700 border-blue-500/20",
  pundit: "bg-gold/15 text-amber-800 border-gold/30",
};

export function PickChip({ prediction, role, pointsAwarded, secondary }: PickChipProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-[11px] font-semibold",
        roleColors[role],
        secondary && "opacity-70"
      )}
    >
      {prediction}
      {pointsAwarded !== undefined && pointsAwarded > 0 && (
        <span className="font-normal opacity-75">+{pointsAwarded}</span>
      )}
    </span>
  );
}
