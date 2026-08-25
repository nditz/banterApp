import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

/**
 * The consent mirror decides whether analytics and AdSense run at all, so it is worth
 * pinning down: nothing is granted until a current-version choice exists.
 */

const store = new Map<string, string>();

function installFakeWindow() {
  store.clear();

  const fakeWindow = Object.assign(new EventTarget(), {
    localStorage: {
      getItem: (key: string) => store.get(key) ?? null,
      setItem: (key: string, value: string) => void store.set(key, value),
      removeItem: (key: string) => void store.delete(key),
    },
  });

  (globalThis as { window?: unknown }).window = fakeWindow;
}

beforeEach(() => {
  installFakeWindow();
  // A fresh module instance per test, so the snapshot cache never leaks across cases.
  vi.resetModules();
});

afterEach(() => {
  delete (globalThis as { window?: unknown }).window;
});

function loadConsent() {
  return import("./consent");
}

describe("consent mirror", () => {
  it("treats an absent choice as no consent", async () => {
    const consent = await loadConsent();

    expect(consent.readStoredConsent()).toBeNull();
    expect(consent.hasAnalyticsConsent()).toBe(false);
    expect(consent.hasMarketingConsent()).toBe(false);
  });

  it("round-trips a stored choice", async () => {
    const consent = await loadConsent();

    consent.writeStoredConsent({ analytics: true, marketing: false });

    expect(consent.hasAnalyticsConsent()).toBe(true);
    expect(consent.hasMarketingConsent()).toBe(false);
    expect(consent.readStoredConsent()?.version).toBe(consent.CONSENT_VERSION);
  });

  it("ignores a choice made against an older consent version", async () => {
    const consent = await loadConsent();

    store.set(
      consent.CONSENT_STORAGE_KEY,
      JSON.stringify({
        analytics: true,
        marketing: true,
        version: "1970-01-01",
        decidedAt: new Date().toISOString(),
      })
    );

    expect(consent.readStoredConsent()).toBeNull();
    expect(consent.hasAnalyticsConsent()).toBe(false);
  });

  it("ignores a malformed record rather than inferring consent", async () => {
    const consent = await loadConsent();

    store.set(consent.CONSENT_STORAGE_KEY, "{not json");
    expect(consent.readStoredConsent()).toBeNull();

    store.set(consent.CONSENT_STORAGE_KEY, JSON.stringify({ analytics: "yes" }));
    expect(consent.readStoredConsent()).toBeNull();
  });

  it("clears back to undecided and notifies subscribers", async () => {
    const consent = await loadConsent();
    const seen: (boolean | null)[] = [];

    consent.subscribeToConsent(() => {
      seen.push(consent.getConsentSnapshot().record?.analytics ?? null);
    });

    consent.writeStoredConsent({ analytics: true, marketing: true });
    consent.clearStoredConsent();

    expect(seen).toEqual([true, null]);
    expect(consent.hasAnalyticsConsent()).toBe(false);
  });

  it("returns a stable snapshot reference until the stored value changes", async () => {
    const consent = await loadConsent();

    const first = consent.getConsentSnapshot();
    expect(consent.getConsentSnapshot()).toBe(first);

    consent.writeStoredConsent({ analytics: true, marketing: true });
    const second = consent.getConsentSnapshot();

    expect(second).not.toBe(first);
    expect(consent.getConsentSnapshot()).toBe(second);
  });

  it("reports the server snapshot as not ready so the banner cannot flash", async () => {
    const consent = await loadConsent();

    expect(consent.getConsentServerSnapshot()).toEqual({ ready: false, record: null });
  });
});
