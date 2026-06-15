import { scorelineToOutcome } from "@/lib/predictionProbabilities";
import type { PredictionOutcome } from "@/lib/reactionEngine";
import type { Prediction } from "@/lib/types";

export interface ResolvedMatchResult {
  actualOutcome: PredictionOutcome;
  actualScoreline?: string;
  label: string;
}

const demoActualResults: Record<string, ResolvedMatchResult> = {
  pred1: {
    actualOutcome: "home",
    actualScoreline: "2-1",
    label: "Japan 2-1 Colombia (Home Win)",
  },
  pred2: {
    actualOutcome: "home",
    actualScoreline: "2-1",
    label: "Netherlands 2-1 Mexico",
  },
  pred3: {
    actualOutcome: "draw",
    actualScoreline: "0-0",
    label: "Portugal 0-0 Uruguay (Draw)",
  },
};

export function resolveMatchResult(prediction: Prediction): ResolvedMatchResult | null {
  const demo = demoActualResults[prediction.id];
  if (demo) return demo;

  if (prediction.pointsAwarded === undefined) return null;

  const label = prediction.match
    ? `${prediction.match.teamA} vs ${prediction.match.teamB}`
    : `Match ${prediction.matchId}`;

  if (prediction.predictionType === "correct_score") {
    const actualScoreline = prediction.predictionValue;
    const [home, away] = actualScoreline.split("-").map((part) => Number(part.trim()));
    if (!Number.isFinite(home) || !Number.isFinite(away)) return null;
    const actualOutcome: PredictionOutcome =
      home > away ? "home" : home < away ? "away" : "draw";
    return {
      actualOutcome,
      actualScoreline,
      label,
    };
  }

  if (prediction.predictionType === "result") {
    const actualOutcome = prediction.pointsAwarded > 0
      ? (prediction.predictionValue as PredictionOutcome)
      : prediction.predictionValue === "home"
        ? "away"
        : prediction.predictionValue === "away"
          ? "home"
          : "draw";
    return { actualOutcome, label };
  }

  if (prediction.predictionType === "double_chance") {
    const wasCorrect = prediction.pointsAwarded > 0;
    return {
      actualOutcome: wasCorrect ? "draw" : "home",
      label,
    };
  }

  return null;
}

export function wasPredictionCorrect(
  prediction: Prediction,
  result: ResolvedMatchResult
): boolean {
  if (prediction.predictionType === "correct_score") {
    return prediction.pointsAwarded === 7;
  }

  if (prediction.predictionType === "result") {
    return prediction.predictionValue === result.actualOutcome;
  }

  if (prediction.predictionType === "double_chance") {
    return (prediction.pointsAwarded ?? 0) > 0;
  }

  return (prediction.pointsAwarded ?? 0) > 0;
}

export function wasExactScoreCorrect(
  prediction: Prediction,
  result: ResolvedMatchResult
): boolean {
  if (prediction.predictionType !== "correct_score") return false;
  return (
    prediction.predictionValue === result.actualScoreline ||
    prediction.pointsAwarded === 7
  );
}

export function pickToOutcome(
  predictionType: Prediction["predictionType"],
  predictionValue: string
): PredictionOutcome | null {
  if (predictionType === "result") {
    if (predictionValue === "home" || predictionValue === "draw" || predictionValue === "away") {
      return predictionValue;
    }
    return null;
  }

  if (predictionType === "correct_score") {
    return scorelineToOutcome(predictionValue);
  }

  if (predictionType === "double_chance") {
    return "draw";
  }

  return null;
}
