"use client";

import { useEffect, useRef, useState } from "react";
import { usePathname } from "next/navigation";
import { useAdvertisingConsent } from "@/hooks/useAdvertisingConsent";
import { cn } from "@/lib/utils";
import { ADSENSE_CLIENT, type AdPlacementKey, resolveAdSlotId } from "@/lib/ads";
import { PRODUCT_METRICS, recordMetric } from "@/lib/metrics";

type AdPlacement = "sidebar" | "feed" | "inline" | "skyscraper";

interface AdSlotProps {
  placement: AdPlacement;
  className?: string;
  /** Placement key from AD_PLACEMENT_KEYS. Determines which AdSense unit is requested. */
  slotId: AdPlacementKey;
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

/**
 * One ad unit. Renders nothing unless advertising consent has been granted, and collapses
 * itself when AdSense reports no fill so the layout never shows a dead placeholder.
 */
type SlotState = "idle" | "requested" | "unfilled";

export function AdSlot({ placement, className, slotId, fill = false }: AdSlotProps) {
  const ref = useRef<HTMLDivElement>(null);
  const insRef = useRef<HTMLModElement>(null);
  const pushedRef = useRef(false);

  const pathname = usePathname();
  const canRequest = useAdvertisingConsent();
  const adUnitId = resolveAdSlotId(slotId);

  // A new route means a new <ins> element, so slot state is keyed by route + placement and
  // anything left over from the previous page reads as "idle" again.
  const slotKey = `${pathname}::${slotId}`;
  const [tracked, setTracked] = useState<{ key: string; state: SlotState }>({
    key: slotKey,
    state: "idle",
  });
  const state: SlotState = tracked.key === slotKey ? tracked.state : "idle";

  useEffect(() => {
    const element = ref.current;
    pushedRef.current = false;
    if (!element || !canRequest) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting || pushedRef.current) return;
        observer.disconnect();
        try {
          (window.adsbygoogle = window.adsbygoogle || []).push({});
          pushedRef.current = true;
          setTracked({ key: slotKey, state: "requested" });
        } catch {
          // AdSense not ready (e.g. blocked or offline) — collapse instead of holding space.
          setTracked({ key: slotKey, state: "unfilled" });
          recordMetric(PRODUCT_METRICS.adInitFailed);
        }
      },
      { rootMargin: "200px" }
    );

    observer.observe(element);
    return () => observer.disconnect();
  }, [canRequest, slotKey]);

  // AdSense marks an unsold impression with data-ad-status="unfilled". Watch for it so the
  // reserved space can be released instead of leaving an empty box on the page.
  useEffect(() => {
    const element = insRef.current;
    if (!element || !canRequest) return;

    let reported = false;
    const check = () => {
      const status = element.getAttribute("data-ad-status");
      if (status !== "unfilled" && status !== "filled") return;
      const next: SlotState = status === "unfilled" ? "unfilled" : "requested";

      // One fill/no-fill count per rendered slot, so placements stay comparable.
      if (!reported) {
        reported = true;
        recordMetric(
          status === "unfilled" ? PRODUCT_METRICS.adSlotUnfilled : PRODUCT_METRICS.adSlotFilled
        );
      }

      setTracked((prev) =>
        prev.key === slotKey && prev.state === next ? prev : { key: slotKey, state: next }
      );
    };

    check();
    const observer = new MutationObserver(check);
    observer.observe(element, { attributes: true, attributeFilter: ["data-ad-status"] });

    return () => observer.disconnect();
  }, [canRequest, slotKey]);

  if (!canRequest || state === "unfilled") {
    return null;
  }

  return (
    <div
      ref={ref}
      className={cn(
        "flex items-center justify-center rounded-lg text-center text-xs text-muted-foreground",
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
      data-ad-slot={slotId}
      data-ad-loaded={state === "requested" ? "true" : "false"}
    >
      <ins
        ref={insRef}
        className="adsbygoogle"
        style={{ display: "block", width: "100%" }}
        data-ad-client={ADSENSE_CLIENT}
        data-ad-slot={adUnitId}
        data-ad-format="auto"
        data-full-width-responsive="true"
      />
    </div>
  );
}
