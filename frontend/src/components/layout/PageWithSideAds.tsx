"use client";

import { AdSlot } from "@/components/ads/AdSlot";
import { useAdvertisingConsent } from "@/hooks/useAdvertisingConsent";
import { cn } from "@/lib/utils";

interface PageWithSideAdsProps {
  children: React.ReactNode;
  className?: string;
}

export function PageWithSideAds({ children, className }: PageWithSideAdsProps) {
  const showRails = useAdvertisingConsent();

  if (!showRails) {
    return (
      <div className={cn("relative mx-auto w-full max-w-[1400px] px-4 sm:px-6", className)}>
        {children}
      </div>
    );
  }

  return (
    <div className={cn("relative w-full", className)}>
      <div className="grid w-full grid-cols-1 xl:grid-cols-[minmax(160px,1fr)_minmax(0,min(100%,1400px))_minmax(160px,1fr)] xl:gap-3 2xl:gap-4">
        <aside
          className="sticky top-14 hidden min-w-[160px] self-start xl:block"
          aria-label="Left advertisements"
        >
          <AdSlot placement="skyscraper" slotId="rail-left" fill />
        </aside>

        <div className="min-w-0 w-full px-4 sm:px-6">
          <div className="mb-3 xl:hidden">
            <AdSlot placement="inline" slotId="display-top" />
          </div>
          {children}
        </div>

        <aside
          className="sticky top-14 hidden min-w-[160px] self-start xl:block"
          aria-label="Right advertisements"
        >
          <AdSlot placement="skyscraper" slotId="rail-right" fill />
        </aside>
      </div>
    </div>
  );
}
