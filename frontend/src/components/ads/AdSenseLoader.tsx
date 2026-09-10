"use client";

import Script from "next/script";
import { useAdvertisingConsent } from "@/hooks/useAdvertisingConsent";
import { ADSENSE_SCRIPT_SRC } from "@/lib/ads";

/**
 * Injects the AdSense script, but only after advertising consent is granted. Until then no
 * Google advertising code is requested at all.
 */
export function AdSenseLoader() {
  const canRequest = useAdvertisingConsent();

  if (!canRequest) {
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
