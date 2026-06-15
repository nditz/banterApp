"use client";

import { useSyncExternalStore } from "react";
import { getBanterMode, type BanterMode } from "@/lib/banterPreferences";

function subscribeBanterMode(onStoreChange: () => void) {
  window.addEventListener("banter-mode-updated", onStoreChange);
  return () => window.removeEventListener("banter-mode-updated", onStoreChange);
}

function getBanterModeSnapshot(): BanterMode {
  return getBanterMode();
}

function getBanterModeServerSnapshot(): BanterMode {
  return "standard";
}

export function useBanterMode() {
  return useSyncExternalStore(
    subscribeBanterMode,
    getBanterModeSnapshot,
    getBanterModeServerSnapshot
  );
}
