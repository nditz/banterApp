import { AdSlot } from "@/components/ads/AdSlot";
import { cn } from "@/lib/utils";

interface PageWithSideAdsProps {
  children: React.ReactNode;
  className?: string;
}

export function PageWithSideAds({ children, className }: PageWithSideAdsProps) {
  return (
    <div className={cn("relative w-full", className)}>
      {/*
        Three-column grid: side rails share all remaining width equally;
        centre column is capped at 1400px. Rails are full viewport height
        below the sticky header so AdSense placeholders fill the space.
      */}
      <div className="grid w-full grid-cols-1 xl:grid-cols-[minmax(0,1fr)_min(100%,1400px)_minmax(0,1fr)] xl:gap-4 2xl:gap-6">
        <aside
          className="sticky top-14 hidden min-h-[calc(100vh-3.5rem)] min-w-0 self-start xl:block"
          aria-label="Left advertisements"
        >
          <AdSlot placement="skyscraper" slotId="rail-left" fill />
        </aside>

        <div className="min-w-0 w-full">{children}</div>

        <aside
          className="sticky top-14 hidden min-h-[calc(100vh-3.5rem)] min-w-0 self-start xl:block"
          aria-label="Right advertisements"
        >
          <AdSlot placement="skyscraper" slotId="rail-right" fill />
        </aside>
      </div>
    </div>
  );
}
