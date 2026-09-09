"use client";

import { AdSlot } from "@/components/ads/AdSlot";
import { canRequestAds, resolveAdSlotId } from "@/lib/ads";
import { cn } from "@/lib/utils";

interface PageWithSideAdsProps {
  children: React.ReactNode;
  className?: string;
}

export function PageWithSideAds({ children, className }: PageWithSideAdsProps) {
  const showLeft = canRequestAds() && Boolean(resolveAdSlotId("rail-left"));
  const showRight = canRequestAds() && Boolean(resolveAdSlotId("rail-right"));

  if (!showLeft && !showRight) {
    return (
      <div className={cn("relative mx-auto w-full max-w-[1400px]", className)}>
        {children}
      </div>
    );
  }

  return (
    <div className={cn("relative w-full", className)}>
      <div
        className={cn(
          "grid w-full xl:gap-4 2xl:gap-6",
          showLeft && showRight
            ? "grid-cols-1 xl:grid-cols-[minmax(0,1fr)_min(100%,1400px)_minmax(0,1fr)]"
            : showLeft
              ? "grid-cols-1 xl:grid-cols-[minmax(0,1fr)_min(100%,1400px)]"
              : "grid-cols-1 xl:grid-cols-[min(100%,1400px)_minmax(0,1fr)]"
        )}
      >
        {showLeft ? (
          <aside
            className="sticky top-14 hidden min-w-0 self-start xl:block"
            aria-label="Left advertisements"
          >
            <AdSlot placement="skyscraper" slotId="rail-left" fill />
          </aside>
        ) : null}

        <div className="min-w-0 w-full">{children}</div>

        {showRight ? (
          <aside
            className="sticky top-14 hidden min-w-0 self-start xl:block"
            aria-label="Right advertisements"
          >
            <AdSlot placement="skyscraper" slotId="rail-right" fill />
          </aside>
        ) : null}
      </div>
    </div>
  );
}
