"use client";

import { Button } from "@/components/ui/button";
import { useAdvertisingConsentState } from "@/hooks/useAdvertisingConsent";
import { useHydrated } from "@/hooks/useHydrated";
import { setAdvertisingConsent } from "@/lib/advertising-consent";

const LABELS = {
  granted: "Ads are on. Google AdSense may set cookies.",
  denied: "Ads are off. No advertising code is loaded.",
  unset: "You haven't chosen yet, so no advertising code is loaded.",
} as const;

/** Lets a visitor revisit the ad choice they made in the consent prompt. */
export function AdConsentSettings() {
  const consent = useAdvertisingConsentState();
  const hydrated = useHydrated();

  return (
    <div className="rounded-lg border border-border p-4">
      <p className="text-sm font-semibold">Advertising preference</p>
      <p className="mt-1 text-sm text-muted-foreground">
        {hydrated ? LABELS[consent] : LABELS.unset}
      </p>
      <div className="mt-3 flex gap-2">
        <Button
          variant={consent === "granted" ? "default" : "outline"}
          size="sm"
          className="h-8 text-xs"
          onClick={() => setAdvertisingConsent("granted")}
        >
          Allow ads
        </Button>
        <Button
          variant={consent === "denied" ? "default" : "outline"}
          size="sm"
          className="h-8 text-xs"
          onClick={() => setAdvertisingConsent("denied")}
        >
          No ads
        </Button>
      </div>
    </div>
  );
}
