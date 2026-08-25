import { apiFetch } from "@/lib/api";
import {
  CONSENT_CHANGED_EVENT,
  hasAnalyticsConsent,
  type ConsentRecord,
} from "@/lib/consent";
import type { AnalyticsEventName, AnalyticsEventProperties } from "./events";

export type { AnalyticsEventName } from "./events";
export { durationBucket } from "./events";

/** Must not exceed AnalyticsEventCatalog.MaxBatchSize on the backend. */
const MAX_BATCH_SIZE = 20;
const FLUSH_DELAY_MS = 5_000;

interface QueuedEvent {
  eventName: AnalyticsEventName;
  properties?: Record<string, string | number | boolean>;
  appVersion?: string;
}

let buffer: QueuedEvent[] = [];
let flushTimer: ReturnType<typeof setTimeout> | null = null;
let listenersAttached = false;

const APP_VERSION = process.env.NEXT_PUBLIC_APP_VERSION;

/**
 * Records a product analytics event.
 *
 * A hard no-op without analytics consent: nothing is buffered, nothing is sent, and no
 * network request is made. Failures are swallowed, so this can be called from any code
 * path without a try/catch.
 */
export function track<E extends AnalyticsEventName>(
  eventName: E,
  properties?: AnalyticsEventProperties[E]
): void {
  if (typeof window === "undefined" || !hasAnalyticsConsent()) {
    return;
  }

  attachListeners();

  buffer.push({
    eventName,
    properties: sanitize(properties),
    appVersion: APP_VERSION,
  });

  if (buffer.length >= MAX_BATCH_SIZE) {
    void flush();
    return;
  }

  scheduleFlush();
}

/** Sends anything buffered. Safe to call when the buffer is empty. */
export async function flush(options: { keepalive?: boolean } = {}): Promise<void> {
  if (flushTimer) {
    clearTimeout(flushTimer);
    flushTimer = null;
  }

  if (buffer.length === 0 || !hasAnalyticsConsent()) {
    buffer = [];
    return;
  }

  const batch = buffer.slice(0, MAX_BATCH_SIZE);
  buffer = buffer.slice(MAX_BATCH_SIZE);

  try {
    await apiFetch("/api/analytics/events", {
      method: "POST",
      keepalive: options.keepalive,
      body: JSON.stringify({ events: batch }),
    });
  } catch {
    // Dropped on purpose. Retrying would risk an unbounded buffer and duplicate events,
    // neither of which is worth it for analytics.
  }
}

function scheduleFlush(): void {
  if (flushTimer) return;
  flushTimer = setTimeout(() => {
    flushTimer = null;
    void flush();
  }, FLUSH_DELAY_MS);
}

function attachListeners(): void {
  if (listenersAttached || typeof window === "undefined") return;
  listenersAttached = true;

  // A tab being hidden is the last reliable moment to send. `keepalive` lets the request
  // outlive the page, which sendBeacon would also do but without our auth headers.
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "hidden") {
      void flush({ keepalive: true });
    }
  });

  window.addEventListener(CONSENT_CHANGED_EVENT, (event) => {
    const record = (event as CustomEvent<ConsentRecord | null>).detail;
    if (record?.analytics !== true) {
      // Withdrawal must also discard anything queued before it.
      buffer = [];
      if (flushTimer) {
        clearTimeout(flushTimer);
        flushTimer = null;
      }
    }
  });
}

/**
 * Drops undefined values and anything that is not a short primitive. The server applies
 * the same rules against its own catalog; this just avoids sending obvious junk.
 */
function sanitize(
  properties: Record<string, unknown> | undefined
): Record<string, string | number | boolean> | undefined {
  if (!properties) return undefined;

  const result: Record<string, string | number | boolean> = {};

  for (const [key, value] of Object.entries(properties)) {
    if (typeof value === "string") {
      if (value.length > 0) result[key] = value.slice(0, 64);
    } else if (typeof value === "number" && Number.isFinite(value)) {
      result[key] = value;
    } else if (typeof value === "boolean") {
      result[key] = value;
    }
  }

  return Object.keys(result).length > 0 ? result : undefined;
}
