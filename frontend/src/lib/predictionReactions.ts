import {
  estimateFixtureProbabilities,
  scorelineToOutcome,
} from "@/lib/predictionProbabilities";
import {
  getPredictionReaction,
  getPostMatchReaction,
  type FixturePredictionContext,
  type PredictionOutcome,
  type PredictionReaction,
} from "@/lib/reactionEngine";
import type { ReactionTone } from "@/reactions/reactionContent";
import type { Prediction } from "@/lib/types";
import {
  pickToOutcome,
  resolveMatchResult,
  wasExactScoreCorrect,
  wasPredictionCorrect,
} from "@/lib/postMatchResults";

const resultLabels: Record<string, string> = {
  home: "Home Win",
  draw: "Draw",
  away: "Away Win",
  home_or_draw: "Home or Draw",
  away_or_draw: "Away or Draw",
  home_or_away: "Home or Away",
};

export function formatPickLabel(
  predictionType: Prediction["predictionType"],
  predictionValue: string,
  homeTeamName: string,
  awayTeamName: string
): string {
  if (predictionType === "result") {
    if (predictionValue === "home") return homeTeamName;
    if (predictionValue === "away") return awayTeamName;
    if (predictionValue === "draw") return "Draw";
  }

  return resultLabels[predictionValue] ?? predictionValue;
}

export function getPreMatchReaction(input: {
  fixtureId: string;
  homeTeamName: string;
  awayTeamName: string;
  predictionType: Prediction["predictionType"];
  predictionValue: string;
  tone?: ReactionTone;
  probabilities?: Record<PredictionOutcome, number>;
}): PredictionReaction | null {
  let userPick = pickToOutcome(input.predictionType, input.predictionValue);

  if (input.predictionType === "correct_score") {
    userPick = scorelineToOutcome(input.predictionValue);
  }

  if (!userPick) return null;

  const probabilities =
    input.probabilities ??
    estimateFixtureProbabilities(input.fixtureId, input.homeTeamName, input.awayTeamName);

  const ctx: FixturePredictionContext = {
    fixtureId: input.fixtureId,
    homeTeamName: input.homeTeamName,
    awayTeamName: input.awayTeamName,
    userPick,
    probabilities,
    predictedScore:
      input.predictionType === "correct_score" ? input.predictionValue : undefined,
    tone: input.tone,
  };

  return getPredictionReaction(ctx);
}

export function getFinishedMatchReaction(
  prediction: Prediction,
  tone?: ReactionTone,
  wasUnderdogPick = false
): PredictionReaction | null {
  const result = resolveMatchResult(prediction);
  if (!result) return null;

  return getPostMatchReaction({
    wasCorrect: wasPredictionCorrect(prediction, result),
    exactScoreCorrect: wasExactScoreCorrect(prediction, result),
    wasUnderdogPick,
    tone,
  });
}

export function isUnderdogPick(
  fixtureId: string,
  homeTeamName: string,
  awayTeamName: string,
  userPick: PredictionOutcome
): boolean {
  const probabilities = estimateFixtureProbabilities(fixtureId, homeTeamName, awayTeamName);
  const favorite = (Object.entries(probabilities).sort((a, b) => b[1] - a[1])[0][0]) as PredictionOutcome;
  const pickedProbability = probabilities[userPick];
  const favoriteProbability = probabilities[favorite];
  return userPick !== favorite && pickedProbability + 12 < favoriteProbability;
}
