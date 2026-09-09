"use client";

import Script from "next/script";
import { canRequestAds, ADSENSE_SCRIPT_SRC } from "@/lib/ads";

/**
 * Loads the AdSense script once for ca-pub-5886846159925642.
 * Individual units are the "ball-take-ads" Display slot rendered by AdSlot.
 */
export function AdSenseLoader() {
  if (!canRequestAds()) {
    return null;
  }

  return (
    <Script
      id="adsense"
      async
      src={ADSENSE_SCRIPT_SRC}
      crossOrigin="anonymous"
      strategy="afterInteractive"
    />
  );
}
