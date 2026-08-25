"use client";

import { useEffect } from "react";
import { track } from "@/lib/analytics";
import { getAnonymousId } from "@/lib/anonymous";

const SESSION_MARKER_KEY = "banter_analytics_session";

/**
 * Fires `session_started` once per browser session. Mounted in the root layout so it
 * covers every entry point, and a hard no-op without analytics consent because `track`
 * refuses to buffer.
 */
export function AnalyticsBootstrap() {
  useEffect(() => {
    let isReturning = false;

    try {
      if (window.sessionStorage.getItem(SESSION_MARKER_KEY)) {
        return;
      }
      window.sessionStorage.setItem(SESSION_MARKER_KEY, "1");
      isReturning = getAnonymousId() !== null;
    } catch {
      // Storage unavailable; fire the event anyway rather than losing it entirely.
    }

    track("session_started", { isReturning });
  }, []);

  return null;
}
