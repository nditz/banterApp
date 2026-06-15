import type { ReactionKey } from "@/reactions/reactionContent";

export type PredictionTheme = "smart" | "safe" | "underdog" | "chaos";

export interface ThemeTokens {
  id: PredictionTheme;
  label: string;
  emoji: string;
  border: string;
  bg: string;
  glow: string;
  text: string;
  ring: string;
}

export const predictionThemes: Record<PredictionTheme, ThemeTokens> = {
  smart: {
    id: "smart",
    label: "Smart pick",
    emoji: "🧠",
    border: "border-smart/50",
    bg: "bg-smart/10",
    glow: "shadow-[0_0_20px_color-mix(in_srgb,var(--smart)_25%,transparent)]",
    text: "text-smart",
    ring: "ring-smart/60",
  },
  safe: {
    id: "safe",
    label: "Balanced pick",
    emoji: "⚖️",
    border: "border-safe/50",
    bg: "bg-safe/10",
    glow: "shadow-[0_0_20px_color-mix(in_srgb,var(--safe)_25%,transparent)]",
    text: "text-safe",
    ring: "ring-safe/60",
  },
  underdog: {
    id: "underdog",
    label: "Bold pick",
    emoji: "🔥",
    border: "border-underdog/50",
    bg: "bg-underdog/10",
    glow: "shadow-[0_0_24px_color-mix(in_srgb,var(--underdog)_30%,transparent)]",
    text: "text-underdog",
    ring: "ring-underdog/60",
  },
  chaos: {
    id: "chaos",
    label: "Chaos pick",
    emoji: "💀",
    border: "border-chaos/50",
    bg: "bg-chaos/10",
    glow: "shadow-[0_0_28px_color-mix(in_srgb,var(--chaos)_35%,transparent)]",
    text: "text-chaos",
    ring: "ring-chaos/60",
  },
};

const keyToTheme: Partial<Record<ReactionKey, PredictionTheme>> = {
  smart_choice: "smart",
  locked_in: "smart",
  playing_safe: "safe",
  against_grain: "underdog",
  delulu_vision: "underdog",
  chaos_pick: "chaos",
  receipts_found: "smart",
  script_writer: "smart",
  brave_but_wrong: "underdog",
  prediction_fraud: "chaos",
};

export function getThemeForReaction(key: ReactionKey): ThemeTokens {
  const themeId = keyToTheme[key] ?? "safe";
  return predictionThemes[themeId];
}
