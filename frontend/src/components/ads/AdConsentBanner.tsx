"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import { useAdvertisingConsentState } from "@/hooks/useAdvertisingConsent";
import { useHydrated } from "@/hooks/useHydrated";
import { ADSENSE_ENABLED } from "@/lib/ads";
import { setAdvertisingConsent } from "@/lib/advertising-consent";

/**
 * First-party consent prompt. Nothing advertising-related loads until the visitor answers,
 * so this is the only thing standing between a new visitor and a zero-tracking page.
 * Accepting the terms of use does not answer this — the two are deliberately separate.
 */
export function AdConsentBanner() {
  const consent = useAdvertisingConsentState();
  // The stored choice is client-only; rendering it during hydration would mismatch.
  const hydrated = useHydrated();

  if (!hydrated || !ADSENSE_ENABLED || consent !== "unset") {
    return null;
  }

  return (
    <div
      role="dialog"
      aria-modal="false"
      aria-labelledby="ad-consent-title"
      className="fixed inset-x-0 bottom-0 z-50 border-t border-border bg-background/95 px-4 py-3 backdrop-blur-sm sm:px-6"
    >
      <div className="mx-auto flex max-w-4xl flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0">
          <p id="ad-consent-title" className="text-sm font-semibold">
            Ads and cookies
          </p>
          <p className="mt-0.5 text-xs leading-relaxed text-muted-foreground">
            We&apos;d like to show ads from Google AdSense, which sets cookies. Ball Takes stays
            free either way — say no and the ad slots simply disappear. See our{" "}
            <Link href="/privacy" className="font-medium text-foreground hover:underline">
              privacy policy
            </Link>
            .
          </p>
        </div>
        <div className="flex shrink-0 gap-2">
          <Button
            variant="outline"
            size="sm"
            className="h-8 flex-1 text-xs sm:flex-none"
            onClick={() => setAdvertisingConsent("denied")}
          >
            No ads
          </Button>
          <Button
            size="sm"
            className="h-8 flex-1 text-xs sm:flex-none"
            onClick={() => setAdvertisingConsent("granted")}
          >
            Allow ads
          </Button>
        </div>
      </div>
    </div>
  );
}
