/**
 * First-party advertising consent, deliberately separate from terms-of-use consent.
 *
 * Accepting the terms is not consent to advertising or tracking, so the default state is
 * "unset" and nothing advertising-related may load until the visitor explicitly grants it.
 * This is opt-in: "unset" behaves like a deny for loading purposes.
 */
const STORAGE_KEY = "balltakes_advertising_consent";
const CHANGE_EVENT = "balltakes-advertising-consent-changed";

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

/** True only after an explicit grant. Never true by default. */
export function hasAdvertisingConsent(): boolean {
  return readAdvertisingConsent() === "granted";
}

/** True while the visitor has not answered the consent prompt yet. */
export function isAdvertisingConsentUnset(): boolean {
  return readAdvertisingConsent() === "unset";
}

export function setAdvertisingConsent(value: "granted" | "denied"): void {
  if (typeof window === "undefined") {
    return;
  }

  try {
    window.localStorage.setItem(STORAGE_KEY, value);
  } catch {
    // Ignore persistence failures — the notification still fires.
  }

  notifyConsentChanged(value);
}

/** Clears the stored choice so the prompt is shown again. */
export function resetAdvertisingConsent(): void {
  if (typeof window === "undefined") {
    return;
  }

  try {
    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Ignore persistence failures.
  }

  notifyConsentChanged("unset");
}

function notifyConsentChanged(value: AdvertisingConsent): void {
  try {
    window.dispatchEvent(new CustomEvent(CHANGE_EVENT, { detail: value }));
  } catch {
    // No CustomEvent in this environment (e.g. a bare test stub) — subscribers just miss it.
  }
}

/** Subscribes to consent changes, including changes made in another tab. */
export function subscribeAdvertisingConsent(onChange: () => void): () => void {
  if (typeof window === "undefined") {
    return () => {};
  }

  const handleStorage = (event: StorageEvent) => {
    if (event.key === null || event.key === STORAGE_KEY) {
      onChange();
    }
  };

  window.addEventListener(CHANGE_EVENT, onChange);
  window.addEventListener("storage", handleStorage);

  return () => {
    window.removeEventListener(CHANGE_EVENT, onChange);
    window.removeEventListener("storage", handleStorage);
  };
}
