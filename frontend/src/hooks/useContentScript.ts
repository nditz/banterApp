"use client";

import { useMutation } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";

export type ScriptPhase = "pre_match" | "post_match";
export type ScriptFormat = "tiktok" | "youtube_short" | "instagram";

export interface PredictionPickSummary {
  matchId?: string;
  teamA: string;
  teamB: string;
  prediction: string;
  predictionType?: string;
  actualResult?: string;
  pointsAwarded?: number;
}

export interface CumulativeScriptRequest {
  phase: ScriptPhase;
  picks: PredictionPickSummary[];
  format?: ScriptFormat;
}

export interface ScriptResult {
  content: string;
  type: string;
  phase: ScriptPhase;
  remainingGenerations?: number;
}

/** Offline fallback in the same TV-studio format the backend produces. */
function stubBroadcastScript(req: CumulativeScriptRequest): string {
  const count = req.picks.length;

  if (req.phase === "pre_match") {
    if (count === 0) {
      return `[STUDIO — COLD OPEN]\nGood evening from the BanterApp studio. The card is empty tonight — make your picks and come back for the full rundown.\n\n[SIGN-OFF — to camera]\nI know ball — watch me.\n\n#WorldCup2026 #BanterApp`;
    }
    const segments = req.picks
      .map(
        (p, i) =>
          `[SEGMENT ${i + 1} — ${p.teamA} v ${p.teamB}]\nTo camera: "My call here is ${p.prediction}. Lock it in."`
      )
      .join("\n\n");
    return `[STUDIO — COLD OPEN]\nGood evening from the BanterApp studio. I'm filing ${count} prediction${count === 1 ? "" : "s"} on the record tonight.\n\n${segments}\n\n[SIGN-OFF — to camera]\nThat's the card. Clip it, post it, hold me to it. I know ball — watch me.\n\n#WorldCup2026 #BanterApp #IKnowBall`;
  }

  if (count === 0) {
    return `[STUDIO — COLD OPEN]\nFull time across the board — but the results desk is empty. Come back once your picks have gone the distance.\n\n#BanterApp`;
  }

  const totalPts = req.picks.reduce((s, p) => s + (p.pointsAwarded ?? 0), 0);
  const wins = req.picks.filter((p) => (p.pointsAwarded ?? 0) > 0).length;
  const segments = req.picks
    .map(
      (p, i) =>
        `[SEGMENT ${i + 1} — ${p.teamA} v ${p.teamB}]\nThe call: ${p.prediction}. The result: ${p.actualResult ?? "to be confirmed"}.\n${
          (p.pointsAwarded ?? 0) > 0
            ? `Verdict: the tape backs the take. +${p.pointsAwarded} points.`
            : "Verdict: football humbles us all. We go again."
        }`
    )
    .join("\n\n");

  return `[STUDIO — COLD OPEN]\nFull time across the board. Let's go through my card, call by call.\n\n[SCOREBOARD]\n${count} match${count === 1 ? "" : "es"} reviewed · ${wins}/${count} calls landed · ${totalPts} points banked.\n\n${segments}\n\n[SIGN-OFF — to camera]\nSame desk, next matchday. I know ball — watch me.\n\n#WorldCup2026 #BanterApp #MatchdayRecap`;
}

async function generateCumulativeScript(
  req: CumulativeScriptRequest
): Promise<ScriptResult> {
  try {
    const script = await apiFetch<{ content: string; remainingGenerations?: number }>(
      "/api/ai/broadcast-script",
      {
        method: "POST",
        body: JSON.stringify({
          phase: req.phase,
          picks: req.picks.map((p) => ({
            matchId: p.matchId ?? null,
            teamA: p.teamA,
            teamB: p.teamB,
            prediction: p.prediction,
            actualResult: p.actualResult ?? null,
            pointsAwarded: p.pointsAwarded ?? null,
          })),
        }),
      }
    );
    return {
      content: script.content,
      type: "broadcast_script",
      phase: req.phase,
      remainingGenerations: script.remainingGenerations,
    };
  } catch (error) {
    if (error instanceof ApiError) {
      return {
        content: stubBroadcastScript(req),
        type: "broadcast_script_stub",
        phase: req.phase,
      };
    }
    throw error;
  }
}

export function useContentScript() {
  return useMutation({
    mutationFn: generateCumulativeScript,
  });
}
