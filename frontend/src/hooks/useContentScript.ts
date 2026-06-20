"use client";

import { useMutation } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";

export type ScriptPhase = "pre_match" | "post_match";
export type ScriptStyle = "full" | "praise" | "burn";
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
  style?: ScriptStyle;
  format?: ScriptFormat;
}

export interface ScriptResult {
  content: string;
  type: string;
  phase: ScriptPhase;
  style?: ScriptStyle;
  remainingGenerations?: number;
}

function stubBroadcastScript(req: CumulativeScriptRequest): string {
  const count = req.picks.length;
  const style = req.style ?? "full";

  if (req.phase === "pre_match") {
    if (count === 0) {
      return `[DESK — COLD OPEN]\nCard's empty — get your picks in before we go live.\n\n#BanterApp #PreMatch`;
    }
    const segments = req.picks
      .map(
        (p, i) =>
          `[SEGMENT ${i + 1} — ${p.teamA} v ${p.teamB}]\nTo camera: "I'm on ${p.prediction}. Lowkey confident, highkey accountable."`
      )
      .join("\n\n");
    return `[DESK — COLD OPEN]\n${count} call${count === 1 ? "" : "s"} on the record before kickoff.\n\n${segments}\n\n#BanterApp #BallTakes`;
  }

  if (count === 0) {
    return style === "praise"
      ? `[STUDIO — PRAISE]\nNo W's to flex yet. Come back when you've got receipts.\n\n#BanterApp`
      : style === "burn"
        ? `[STUDIO — BURN]\nNothing to roast yet. Suspiciously perfect or no results yet.\n\n#BanterApp`
        : `[STUDIO — FULL RECAP]\nFull time but the desk is empty.\n\n#BanterApp`;
  }

  const filtered =
    style === "praise"
      ? req.picks.filter((p) => (p.pointsAwarded ?? 0) > 0)
      : style === "burn"
        ? req.picks.filter((p) => (p.pointsAwarded ?? 0) <= 0)
        : req.picks;

  const totalPts = filtered.reduce((s, p) => s + (p.pointsAwarded ?? 0), 0);
  const wins = filtered.filter((p) => (p.pointsAwarded ?? 0) > 0).length;
  const segments = filtered
    .map(
      (p, i) =>
        `[SEGMENT ${i + 1} — ${p.teamA} v ${p.teamB}]\nPick: ${p.prediction}. Result: ${p.actualResult ?? "TBC"}.\n${
          (p.pointsAwarded ?? 0) > 0
            ? `Verdict: generational read. +${p.pointsAwarded} pts.`
            : "Verdict: cooked. Clip the apology."
        }`
    )
    .join("\n\n");

  const header =
    style === "praise"
      ? `[STUDIO — PRAISE CUT]\n${wins} banger${wins === 1 ? "" : "s"} to flex.`
      : style === "burn"
        ? `[STUDIO — BURN CUT]\n${filtered.length} pick${filtered.length === 1 ? "" : "s"} on blast.`
        : `[STUDIO — FULL RECAP]\n${wins}/${filtered.length} landed · ${totalPts} pts.`;

  return `${header}\n\n${segments}\n\n#BanterApp #BallTakes`;
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
          style: req.phase === "post_match" ? (req.style ?? "full") : null,
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
      style: req.style,
      remainingGenerations: script.remainingGenerations,
    };
  } catch (error) {
    if (error instanceof ApiError) {
      return {
        content: stubBroadcastScript(req),
        type: "broadcast_script_stub",
        phase: req.phase,
        style: req.style,
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
