import { cn } from "@/lib/utils";

interface SectionHeaderProps {
  /** Small uppercase label above the title. */
  eyebrow?: string;
  title: string;
  description?: React.ReactNode;
  action?: React.ReactNode;
  /** Heading level, so pages keep a valid document outline. */
  as?: "h1" | "h2" | "h3";
  id?: string;
  className?: string;
}

/** Standard page/section heading block. Keeps type scale and spacing consistent. */
export function SectionHeader({
  eyebrow,
  title,
  description,
  action,
  as: Heading = "h2",
  id,
  className,
}: SectionHeaderProps) {
  return (
    <div className={cn("flex items-start justify-between gap-3", className)}>
      <div className="min-w-0">
        {eyebrow ? (
          <p className="text-[11px] font-bold uppercase tracking-widest text-muted-foreground">
            {eyebrow}
          </p>
        ) : null}
        <Heading
          id={id}
          className={cn(
            "font-display font-semibold",
            Heading === "h1" ? "text-xl sm:text-2xl" : "text-base sm:text-lg",
            eyebrow && "mt-1.5"
          )}
        >
          {title}
        </Heading>
        {description ? (
          <p className="mt-1.5 text-sm leading-relaxed text-muted-foreground">
            {description}
          </p>
        ) : null}
      </div>
      {action ? <div className="shrink-0">{action}</div> : null}
    </div>
  );
}

interface PageContainerProps {
  children: React.ReactNode;
  /** Content width. "narrow" suits reading pages, "wide" suits dashboards. */
  width?: "narrow" | "default" | "wide";
  className?: string;
}

const widthClass = {
  narrow: "max-w-3xl",
  default: "max-w-5xl",
  wide: "max-w-[1400px]",
} as const;

/** Consistent page gutters and max width across routes. */
export function PageContainer({
  children,
  width = "default",
  className,
}: PageContainerProps) {
  return (
    <div className={cn("mx-auto w-full space-y-6", widthClass[width], className)}>
      {children}
    </div>
  );
}
