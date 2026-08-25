"use client";

import { useCallback, useSyncExternalStore } from "react";
import { apiFetch } from "@/lib/api";
import {
  clearStoredConsent,
  getConsentServerSnapshot,
  getConsentSnapshot,
  subscribeToConsent,
  writeStoredConsent,
  type ConsentChoice,
  type ConsentRecord,
  type ConsentStateResponse,
} from "@/lib/consent";

export interface UseConsentResult {
  /** Null until the mirror has been read on the client, then null when undecided. */
  consent: ConsentRecord | null;
  /** False during the first client render, so the banner never flashes on hydration. */
  ready: boolean;
  analyticsAllowed: boolean;
  marketingAllowed: boolean;
  save: (choice: ConsentChoice) => Promise<void>;
  acceptAll: () => Promise<void>;
  rejectAll: () => Promise<void>;
  reopen: () => void;
}

export function useConsent(): UseConsentResult {
  const { ready, record: consent } = useSyncExternalStore(
    subscribeToConsent,
    getConsentSnapshot,
    getConsentServerSnapshot
  );

  const save = useCallback(async (choice: ConsentChoice) => {
    // Persist locally first so the UI responds immediately and a failed request does
    // not leave the banner stuck open.
    writeStoredConsent(choice);

    try {
      await apiFetch<ConsentStateResponse>("/api/consent", {
        method: "POST",
        body: JSON.stringify({
          analytics: choice.analytics,
          marketing: choice.marketing,
        }),
      });
    } catch {
      // The local mirror still governs client behaviour, and the choice is re-sent on
      // the next decision. Never surface a consent write failure to the user.
    }
  }, []);

  const acceptAll = useCallback(
    () => save({ analytics: true, marketing: true }),
    [save]
  );

  const rejectAll = useCallback(
    () => save({ analytics: false, marketing: false }),
    [save]
  );

  const reopen = useCallback(() => {
    clearStoredConsent();
  }, []);

  return {
    consent,
    ready,
    analyticsAllowed: consent?.analytics === true,
    marketingAllowed: consent?.marketing === true,
    save,
    acceptAll,
    rejectAll,
    reopen,
  };
}
