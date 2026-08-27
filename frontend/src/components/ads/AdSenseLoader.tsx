"use client";

import Script from "next/script";
import { ADSENSE_ENABLED, ADSENSE_SCRIPT_SRC } from "@/lib/ads";

/**
 * Loads the AdSense script once. Auto ads (configured in the AdSense dashboard)
 * inject units without requiring per-slot ids.
 */
export function AdSenseLoader() {
  if (!ADSENSE_ENABLED) {
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
