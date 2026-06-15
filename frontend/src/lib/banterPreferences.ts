import type { ReactionTone } from "@/reactions/reactionContent";

export type BanterMode = ReactionTone;

const BANTER_MODE_KEY = "banter_mode";

export function getBanterMode(): BanterMode {
  if (typeof window === "undefined") return "standard";
  const stored = localStorage.getItem(BANTER_MODE_KEY);
  if (stored === "family" || stored === "standard" || stored === "spicy") {
    return stored;
  }
  return "standard";
}

export function setBanterMode(mode: BanterMode): void {
  if (typeof window === "undefined") return;
  localStorage.setItem(BANTER_MODE_KEY, mode);
  window.dispatchEvent(new CustomEvent("banter-mode-updated", { detail: mode }));
}
