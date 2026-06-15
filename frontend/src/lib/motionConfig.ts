/** Shared motion timings — tuned to be readable, not snappy. */
export const motionEase = [0.22, 1, 0.36, 1] as const;

export const celebrationTiming = {
  /** Delay before the reaction card appears after the flash banner */
  cardDelay: 0.55,
  /** How long the flash banner stays visually prominent */
  flashDuration: 0.65,
  /** Reaction card entrance */
  cardDuration: 0.7,
  /** Emoji pop on the flash banner */
  emojiDuration: 0.85,
  /** Border glow pulse cycles on the reaction card */
  glowPulseCycles: 3,
  glowPulseDuration: 1.4,
} as const;

export const celebrationSpring = {
  type: "spring" as const,
  stiffness: 140,
  damping: 18,
  mass: 0.9,
};
