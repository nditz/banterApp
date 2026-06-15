"use client";

import { motion, useReducedMotion } from "framer-motion";
import type { PredictionTheme } from "@/lib/predictionThemes";
import { predictionThemes } from "@/lib/predictionThemes";
import { cn } from "@/lib/utils";

interface PredictionPickFlashProps {
  theme: PredictionTheme;
  message?: string;
  className?: string;
}

export function PredictionPickFlash({
  theme,
  message = "Pick locked in.",
  className,
}: PredictionPickFlashProps) {
  const reduceMotion = useReducedMotion();
  const tokens = predictionThemes[theme];

  return (
    <motion.div
      initial={reduceMotion ? false : { opacity: 0, scale: 0.92, y: 6 }}
      animate={{ opacity: 1, scale: 1, y: 0 }}
      transition={{ type: "spring", stiffness: 380, damping: 24 }}
      className={cn(
        "flex items-center gap-2 rounded-lg border px-3 py-2 text-xs font-semibold",
        tokens.border,
        tokens.bg,
        tokens.text,
        !reduceMotion && tokens.glow,
        className
      )}
      role="status"
    >
      <motion.span
        initial={reduceMotion ? false : { scale: 0 }}
        animate={{ scale: 1 }}
        transition={{ type: "spring", stiffness: 500, damping: 15, delay: 0.05 }}
        aria-hidden
      >
        {tokens.emoji}
      </motion.span>
      {message}
    </motion.div>
  );
}
