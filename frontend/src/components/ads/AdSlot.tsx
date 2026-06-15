"use client";

import { useEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils";

type AdPlacement = "sidebar" | "feed" | "inline" | "skyscraper";

interface AdSlotProps {
  placement: AdPlacement;
  className?: string;
  slotId?: string;
  /** Stretch to fill the parent rail (side skyscrapers). */
  fill?: boolean;
}

const placementLabels: Record<AdPlacement, string> = {
  sidebar: "Sidebar Ad",
  feed: "Feed Ad",
  inline: "Inline Ad",
  skyscraper: "Skyscraper Ad",
};

export function AdSlot({ placement, className, slotId, fill = false }: AdSlotProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const element = ref.current;
    if (!element) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setVisible(true);
          observer.disconnect();
        }
      },
      { rootMargin: "200px" }
    );

    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  return (
    <div
      ref={ref}
      className={cn(
        "flex items-center justify-center rounded-lg border border-dashed border-border bg-muted/40 text-center text-xs text-muted-foreground",
        !fill && "min-h-[90px]",
        placement === "sidebar" && !fill && "min-h-[250px]",
        placement === "skyscraper" && !fill && "min-h-[600px]",
        placement === "feed" && !fill && "min-h-[120px]",
        fill && "h-full min-h-[calc(100vh-3.5rem)] w-full",
        className
      )}
      role="complementary"
      aria-label={`Advertisement: ${placementLabels[placement]}`}
      data-ad-placement={placement}
      data-ad-slot={slotId ?? `banter-${placement}`}
      data-ad-loaded={visible ? "true" : "false"}
    >
      {visible ? (
        <div className="px-4 py-2">
          <p className="font-medium text-muted-foreground/80">AdSense Placeholder</p>
          <p className="mt-1 text-[10px] uppercase tracking-wide">
            {placementLabels[placement]}
            {slotId ? ` · ${slotId}` : ""}
          </p>
        </div>
      ) : (
        <span className="sr-only">Loading advertisement</span>
      )}
    </div>
  );
}
