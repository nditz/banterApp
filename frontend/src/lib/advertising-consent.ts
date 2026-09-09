/**
 * Advertising consent is separate from terms-of-use consent.
 * Phase 1 stopgap: no CMP yet, so ads are not requested until a later
 * phase records an explicit grant. Do not treat terms acceptance as ad consent.
 */
const STORAGE_KEY = "balltakes_advertising_consent";

export type AdvertisingConsent = "granted" | "denied" | "unset";

export function readAdvertisingConsent(): AdvertisingConsent {
  if (typeof window === "undefined") {
    return "unset";
  }

  try {
    const value = window.localStorage.getItem(STORAGE_KEY);
    if (value === "granted" || value === "denied") {
      return value;
    }
  } catch {
    // Privacy mode / blocked storage.
  }

  return "unset";
}

export function hasAdvertisingConsent(): boolean {
  return readAdvertisingConsent() === "granted";
}

export function setAdvertisingConsent(value: "granted" | "denied"): void {
  if (typeof window === "undefined") {
    return;
  }

  try {
    window.localStorage.setItem(STORAGE_KEY, value);
  } catch {
    // Ignore persistence failures; ads stay off.
  }
}
