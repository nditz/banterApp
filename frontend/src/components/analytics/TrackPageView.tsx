"use client";

import { useEffect, useRef } from "react";
import { track } from "@/lib/analytics";
import type { AnalyticsEventName } from "@/lib/analytics";
import type { AnalyticsEventProperties } from "@/lib/analytics/events";

/**
 * Fires a view event once per mount. Lets server components record a page view without
 * becoming client components themselves.
 */
export function TrackPageView<E extends AnalyticsEventName>({
  event,
  properties,
}: {
  event: E;
  properties?: AnalyticsEventProperties[E];
}) {
  const fired = useRef(false);

  useEffect(() => {
    if (fired.current) return;
    fired.current = true;
    track(event, properties);
    // Deliberately mount-only: re-firing on a prop change would inflate view counts.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return null;
}
