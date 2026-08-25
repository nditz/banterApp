import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { durationBucket } from "./events";

/**
 * The behaviour that must never regress: without analytics consent, `track()` performs
 * no request at all — not a rejected one, not an empty one.
 */

const { apiFetchMock } = vi.hoisted(() => ({ apiFetchMock: vi.fn() }));

vi.mock("@/lib/api", () => ({ apiFetch: apiFetchMock }));

const store = new Map<string, string>();

function installFakeBrowser() {
  store.clear();

  const fakeWindow = Object.assign(new EventTarget(), {
    localStorage: {
      getItem: (key: string) => store.get(key) ?? null,
      setItem: (key: string, value: string) => void store.set(key, value),
      removeItem: (key: string) => void store.delete(key),
    },
  });

  (globalThis as { window?: unknown }).window = fakeWindow;
  (globalThis as { document?: unknown }).document = new EventTarget();
}

beforeEach(() => {
  installFakeBrowser();
  apiFetchMock.mockReset();
  apiFetchMock.mockResolvedValue({ accepted: 0, dropped: 0 });
  vi.resetModules();
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
  delete (globalThis as { window?: unknown }).window;
  delete (globalThis as { document?: unknown }).document;
});

describe("track()", () => {
  it("sends nothing when no choice has been made", async () => {
    const { track, flush } = await import("./index");

    track("session_started", { isReturning: false });
    track("prediction_created", { matchweek: 3 });

    await vi.advanceTimersByTimeAsync(30_000);
    await flush();

    expect(apiFetchMock).not.toHaveBeenCalled();
  });

  it("sends nothing when analytics was explicitly refused", async () => {
    const consent = await import("@/lib/consent");
    consent.writeStoredConsent({ analytics: false, marketing: true });

    const { track, flush } = await import("./index");
    track("session_started", { isReturning: true });

    await vi.advanceTimersByTimeAsync(30_000);
    await flush();

    expect(apiFetchMock).not.toHaveBeenCalled();
  });

  it("batches events and flushes once consent is granted", async () => {
    const consent = await import("@/lib/consent");
    consent.writeStoredConsent({ analytics: true, marketing: false });

    const { track } = await import("./index");
    track("session_started", { isReturning: false });
    track("landing_viewed", { variant: "default" });

    expect(apiFetchMock).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(30_000);

    expect(apiFetchMock).toHaveBeenCalledTimes(1);

    const [path, init] = apiFetchMock.mock.calls[0] as [string, RequestInit];
    expect(path).toBe("/api/analytics/events");

    const payload = JSON.parse(String(init.body)) as {
      events: { eventName: string }[];
    };
    expect(payload.events.map((e) => e.eventName)).toEqual([
      "session_started",
      "landing_viewed",
    ]);
  });

  it("discards anything buffered when consent is withdrawn", async () => {
    const consent = await import("@/lib/consent");
    consent.writeStoredConsent({ analytics: true, marketing: false });

    const { track, flush } = await import("./index");
    track("session_started", { isReturning: false });

    consent.writeStoredConsent({ analytics: false, marketing: false });

    await vi.advanceTimersByTimeAsync(30_000);
    await flush();

    expect(apiFetchMock).not.toHaveBeenCalled();
  });

  it("drops property values that are not short primitives", async () => {
    const consent = await import("@/lib/consent");
    consent.writeStoredConsent({ analytics: true, marketing: false });

    const { track } = await import("./index");
    track("prediction_created", {
      matchweek: 7,
      predictionType: "x".repeat(200),
    });

    await vi.advanceTimersByTimeAsync(30_000);

    const [, init] = apiFetchMock.mock.calls[0] as [string, RequestInit];
    const payload = JSON.parse(String(init.body)) as {
      events: { properties: Record<string, unknown> }[];
    };

    expect(payload.events[0].properties.matchweek).toBe(7);
    expect(String(payload.events[0].properties.predictionType)).toHaveLength(64);
  });
});

describe("durationBucket", () => {
  it("never returns a raw timing", () => {
    expect(durationBucket(120)).toBe("under_1s");
    expect(durationBucket(2_500)).toBe("1_3s");
    expect(durationBucket(9_999)).toBe("3_10s");
    expect(durationBucket(29_999)).toBe("10_30s");
    expect(durationBucket(120_000)).toBe("over_30s");
  });
});
