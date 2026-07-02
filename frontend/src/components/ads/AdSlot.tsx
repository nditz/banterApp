"use client";

import { useEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils";
import { ADSENSE_CLIENT, ADSENSE_ENABLED, resolveAdSlotId } from "@/lib/ads";

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

declare global {
  interface Window {
    adsbygoogle?: unknown[];
  }
}

export function AdSlot({ placement, className, slotId, fill = false }: AdSlotProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [visible, setVisible] = useState(false);
  const pushedRef = useRef(false);

  const adUnitId = resolveAdSlotId(slotId);
  const isLiveAd = ADSENSE_ENABLED && Boolean(adUnitId);

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

  useEffect(() => {
    if (!visible || !isLiveAd || pushedRef.current) return;
    try {
      (window.adsbygoogle = window.adsbygoogle || []).push({});
      pushedRef.current = true;
    } catch {
      // AdSense not ready (e.g. blocked or offline) — ignore.
    }
  }, [visible, isLiveAd]);

  return (
    <div
      ref={ref}
      className={cn(
        "flex items-center justify-center rounded-lg text-center text-xs text-muted-foreground",
        !isLiveAd && "border border-dashed border-border bg-muted/40",
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
      {isLiveAd && visible ? (
        <ins
          className="adsbygoogle"
          style={{ display: "block", width: "100%", height: "100%" }}
          data-ad-client={ADSENSE_CLIENT}
          data-ad-slot={adUnitId}
          data-ad-format="auto"
          data-full-width-responsive="true"
        />
      ) : visible ? (
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
