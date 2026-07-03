"use client";

import { Lock } from "lucide-react";
import { cn } from "@/lib/utils";

export function PredictionLockBanner({
  isLocked,
  lockDeadline,
  className,
}: {
  isLocked: boolean;
  lockDeadline: string | null;
  className?: string;
}) {
  if (isLocked) {
    return (
      <div
        className={cn(
          "flex items-center gap-2 rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive",
          className
        )}
      >
        <Lock className="h-4 w-4 shrink-0" />
        Predictions are locked. You can no longer edit your picks.
      </div>
    );
  }

  if (!lockDeadline) return null;

  const deadline = new Date(lockDeadline);
  const formatted = deadline.toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });

  return (
    <div
      className={cn(
        "rounded-md border border-border bg-muted/30 px-3 py-2 text-sm text-muted-foreground",
        className
      )}
    >
      Edit until <span className="font-medium text-foreground">{formatted}</span>
    </div>
  );
}
