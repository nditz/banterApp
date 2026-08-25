/**
 * Consent categories and the local mirror of the server-side record.
 *
 * The server copy in `consent_preferences` is authoritative and is what gates the
 * analytics ingest endpoint. This mirror exists so the banner does not flash on every
 * page load and so `track()` can no-op without waiting for a round trip. If the two
 * disagree, the server wins; a cleared mirror simply shows the banner again.
 */

export const CONSENT_STORAGE_KEY = "banter_consent";

/** Bump alongside `Privacy:ConsentVersion` in the backend configuration. */
export const CONSENT_VERSION = "2026-08-25";

export interface ConsentChoice {
  analytics: boolean;
  marketing: boolean;
}

export interface ConsentRecord extends ConsentChoice {
  version: string;
  decidedAt: string;
}

/** Server response shape for GET/POST /api/consent. */
export interface ConsentStateResponse {
  recorded: boolean;
  consentVersion: string;
  analyticsAllowed: boolean;
  marketingAllowed: boolean;
  updatedAt: string | null;
  isCurrentVersion: boolean;
  currentConsentVersion: string;
  analyticsCategoryEnabled: boolean;
  marketingCategoryEnabled: boolean;
}

export const CONSENT_DENIED: ConsentChoice = { analytics: false, marketing: false };

export function readStoredConsent(): ConsentRecord | null {
  if (typeof window === "undefined") return null;

  try {
    const raw = window.localStorage.getItem(CONSENT_STORAGE_KEY);
    if (!raw) return null;

    const parsed = JSON.parse(raw) as Partial<ConsentRecord>;
    if (typeof parsed.analytics !== "boolean" || typeof parsed.marketing !== "boolean") {
      return null;
    }

    // A choice made against an older notice is stale; ask again rather than assuming.
    if (parsed.version !== CONSENT_VERSION) {
      return null;
    }

    return {
      analytics: parsed.analytics,
      marketing: parsed.marketing,
      version: parsed.version,
      decidedAt: parsed.decidedAt ?? new Date().toISOString(),
    };
  } catch {
    return null;
  }
}

export function writeStoredConsent(choice: ConsentChoice): ConsentRecord {
  const record: ConsentRecord = {
    ...choice,
    version: CONSENT_VERSION,
    decidedAt: new Date().toISOString(),
  };

  if (typeof window !== "undefined") {
    try {
      window.localStorage.setItem(CONSENT_STORAGE_KEY, JSON.stringify(record));
    } catch {
      // Storage can be unavailable in private mode. The banner will reappear, which is
      // the correct fallback.
    }
  }

  notifyConsentChanged(record);
  return record;
}

export function clearStoredConsent(): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.removeItem(CONSENT_STORAGE_KEY);
  } catch {
    // ignore
  }
  notifyConsentChanged(null);
}

export const CONSENT_CHANGED_EVENT = "banter:consent-changed";

/**
 * Lets non-React consumers (the analytics buffer, the AdSense loader) react to a
 * decision without threading props through the tree.
 */
function notifyConsentChanged(record: ConsentRecord | null): void {
  if (typeof window === "undefined") return;
  window.dispatchEvent(
    new CustomEvent<ConsentRecord | null>(CONSENT_CHANGED_EVENT, { detail: record })
  );
}

/**
 * Snapshot plumbing for `useSyncExternalStore`. `ready` is false in the server snapshot
 * so the banner renders nothing until hydration has swapped in the real stored value.
 */
export interface ConsentSnapshot {
  ready: boolean;
  record: ConsentRecord | null;
}

const SERVER_CONSENT_SNAPSHOT: ConsentSnapshot = { ready: false, record: null };

let cachedRaw: string | null | undefined;
let cachedSnapshot: ConsentSnapshot = SERVER_CONSENT_SNAPSHOT;

export function getConsentServerSnapshot(): ConsentSnapshot {
  return SERVER_CONSENT_SNAPSHOT;
}

/** Must stay referentially stable between changes or React will re-render forever. */
export function getConsentSnapshot(): ConsentSnapshot {
  let raw: string | null = null;
  try {
    raw = window.localStorage.getItem(CONSENT_STORAGE_KEY);
  } catch {
    raw = null;
  }

  if (raw !== cachedRaw || !cachedSnapshot.ready) {
    cachedRaw = raw;
    cachedSnapshot = { ready: true, record: readStoredConsent() };
  }

  return cachedSnapshot;
}

export function subscribeToConsent(onStoreChange: () => void): () => void {
  window.addEventListener(CONSENT_CHANGED_EVENT, onStoreChange);
  window.addEventListener("storage", onStoreChange);
  return () => {
    window.removeEventListener(CONSENT_CHANGED_EVENT, onStoreChange);
    window.removeEventListener("storage", onStoreChange);
  };
}

export function hasAnalyticsConsent(): boolean {
  return readStoredConsent()?.analytics === true;
}

export function hasMarketingConsent(): boolean {
  return readStoredConsent()?.marketing === true;
}
