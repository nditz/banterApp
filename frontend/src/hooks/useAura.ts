"use client";

import { useCallback, useSyncExternalStore } from "react";
import { applyAuraDelta, getAuraTotal } from "@/lib/aura";

function subscribeAura(onStoreChange: () => void) {
  window.addEventListener("aura-updated", onStoreChange);
  return () => window.removeEventListener("aura-updated", onStoreChange);
}

function getAuraSnapshot() {
  return getAuraTotal();
}

function getAuraServerSnapshot() {
  return 0;
}

export function useAura() {
  const aura = useSyncExternalStore(
    subscribeAura,
    getAuraSnapshot,
    getAuraServerSnapshot
  );

  const award = useCallback((delta: number) => {
    applyAuraDelta(delta);
  }, []);

  return { aura, award };
}
