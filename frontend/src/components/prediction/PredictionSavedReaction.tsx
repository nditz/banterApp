"use client";

import { PredictionCelebration } from "@/components/prediction/PredictionCelebration";
import type { PredictionReaction } from "@/lib/reactionEngine";

interface PredictionSavedReactionProps {
  reaction: PredictionReaction;
  userName: string;
  fixture: string;
  pick: string;
  probabilityContext?: string;
  leagueName?: string;
}

/** @deprecated Use PredictionCelebration directly — kept for backwards compatibility */
export function PredictionSavedReaction(props: PredictionSavedReactionProps) {
  return <PredictionCelebration {...props} />;
}
