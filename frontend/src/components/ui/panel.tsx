import { cn } from "@/lib/utils";

type PanelAccent = "pitch" | "gold" | "flare";

interface PanelProps {
  children: React.ReactNode;
  className?: string;
  bodyClassName?: string;
  title: string;
  id?: string;
  subtitle?: string;
  action?: React.ReactNode;
  accent?: PanelAccent;
}

const accentClass: Record<PanelAccent, string> = {
  pitch: "panel-accent-pitch",
  gold: "panel-accent-gold",
  flare: "panel-accent-flare",
};

export function Panel({
  children,
  className,
  bodyClassName,
  title,
  id,
  subtitle,
  action,
  accent = "gold",
}: PanelProps) {
  return (
    <section
      className={cn("content-panel", accentClass[accent], className)}
      aria-labelledby={id}
    >
      <div className="content-panel-header flex items-center justify-between gap-2 bg-gradient-to-r from-brand to-brand-muted">
        <div>
          <h2 id={id} className="content-panel-title font-display font-semibold">
            {title}
          </h2>
          {subtitle && (
            <p className="mt-0.5 text-[11px] text-brand-foreground/70">
              {subtitle}
            </p>
          )}
        </div>
        {action}
      </div>
      <div className={cn("content-panel-body", bodyClassName)}>{children}</div>
    </section>
  );
}
