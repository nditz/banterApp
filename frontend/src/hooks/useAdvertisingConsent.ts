"use client";

import { useSyncExternalStore } from "react";
import { canRequestAds } from "@/lib/ads";
import {
  readAdvertisingConsent,
  subscribeAdvertisingConsent,
  type AdvertisingConsent,
} from "@/lib/advertising-consent";

function getServerSnapshot(): boolean {
  return false;
}

/**
 * True when ads may be requested. Re-renders as soon as the visitor answers the consent
 * prompt, so slots appear or disappear without a reload.
 */
export function useAdvertisingConsent(): boolean {
  return useSyncExternalStore(
    subscribeAdvertisingConsent,
    canRequestAds,
    getServerSnapshot
  );
}

function getConsentServerSnapshot(): AdvertisingConsent {
  return "unset";
}

/** The raw consent choice, for UI that needs to distinguish "unset" from "denied". */
export function useAdvertisingConsentState(): AdvertisingConsent {
  return useSyncExternalStore(
    subscribeAdvertisingConsent,
    readAdvertisingConsent,
    getConsentServerSnapshot
  );
}
