import type { LucideIcon } from "lucide-react";
import { AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: React.ReactNode;
  className?: string;
  /** Compact spacing for use inside a panel body rather than a full page. */
  dense?: boolean;
}

/**
 * Designed placeholder for "nothing here yet". Never render a bare sentence for an
 * empty module — the homepage rule is that empty states stay intentional.
 */
export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
  className,
  dense = false,
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center rounded-lg border border-dashed border-border text-center",
        dense ? "gap-1.5 px-4 py-6" : "gap-2 px-6 py-10",
        className
      )}
    >
      {Icon ? (
        <Icon className="size-5 text-muted-foreground" aria-hidden />
      ) : null}
      <p className={cn("font-semibold", dense ? "text-sm" : "text-base")}>{title}</p>
      {description ? (
        <p className="max-w-sm text-xs leading-relaxed text-muted-foreground">
          {description}
        </p>
      ) : null}
      {action ? <div className="mt-1">{action}</div> : null}
    </div>
  );
}

interface ErrorStateProps {
  title?: string;
  description?: string;
  onRetry?: () => void;
  retryLabel?: string;
  className?: string;
  dense?: boolean;
}

/** Designed failure state with an optional retry affordance. */
export function ErrorState({
  title = "Something went wrong",
  description,
  onRetry,
  retryLabel = "Try again",
  className,
  dense = false,
}: ErrorStateProps) {
  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col items-center justify-center rounded-lg border border-destructive/40 bg-destructive/5 text-center",
        dense ? "gap-1.5 px-4 py-5" : "gap-2 px-6 py-8",
        className
      )}
    >
      <AlertTriangle className="size-5 text-destructive" aria-hidden />
      <p className={cn("font-semibold", dense ? "text-sm" : "text-base")}>{title}</p>
      {description ? (
        <p className="max-w-sm text-xs leading-relaxed text-muted-foreground">
          {description}
        </p>
      ) : null}
      {onRetry ? (
        <Button variant="outline" size="sm" className="mt-1 h-8 text-xs" onClick={onRetry}>
          {retryLabel}
        </Button>
      ) : null}
    </div>
  );
}
