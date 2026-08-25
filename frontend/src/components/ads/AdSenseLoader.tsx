"use client";

import Script from "next/script";
import { useConsent } from "@/hooks/useConsent";
import { ADSENSE_ENABLED, ADSENSE_SCRIPT_SRC } from "@/lib/ads";

/**
 * Loads the AdSense script only once advertising consent has been given. Prior to a
 * decision, and after a refusal, no request is made to Google at all.
 */
export function AdSenseLoader() {
  const { ready, marketingAllowed } = useConsent();

  if (!ADSENSE_ENABLED || !ready || !marketingAllowed) {
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
