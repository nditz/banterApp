import { apiFetch } from "@/lib/api";

/**
 * Product funnel counters. These are anonymous aggregate counts recorded server-side —
 * no identifiers are sent, and the server rejects any key outside its allowlist.
 */
export const PRODUCT_METRICS = {
  predictionMade: "prediction_made",
  receiptViewed: "receipt_viewed",
  studioOpened: "studio_opened",
  contentGenerated: "content_generated",
  contentExported: "content_exported",
  leagueJoined: "league_joined",
  punditFollowed: "pundit_followed",
  returnedAfterResult: "returned_after_result",
  adInitFailed: "ad_init_failed",
  adSlotFilled: "ad_slot_filled",
  adSlotUnfilled: "ad_slot_unfilled",
} as const;

export type ProductMetricKey =
  (typeof PRODUCT_METRICS)[keyof typeof PRODUCT_METRICS];

/** Fire-and-forget. Metrics must never interrupt or slow down a user action. */
export function recordMetric(key: ProductMetricKey): void {
  if (typeof window === "undefined") return;

  void apiFetch("/api/metrics/event", {
    method: "POST",
    body: JSON.stringify({ key }),
  }).catch(() => {
    // Counters are best-effort.
  });
}
