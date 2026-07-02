"use client";

import { useMutation } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";
import { findPersonaBySlug } from "@/lib/pundit-personas";
import type { Match } from "@/lib/types";

export type PunditScriptPhase = "pre_match" | "post_match";
export type PunditScriptDuration = 30 | 60 | 90;

export interface PunditScriptRequest {
  matchId: string;
  phase: PunditScriptPhase;
  styleSlug: string;
  duration?: PunditScriptDuration;
  match?: Match;
}

export interface PunditScriptResult {
  content: string;
  type: string;
  phase: PunditScriptPhase;
  styleSlug: string;
  remainingGenerations?: number;
}

function buildScene(
  number: number,
  title: string,
  duration: string,
  scene: string,
  visual: string,
  tone: string,
  camera: string,
  dialogue: string
): string {
  return (
    `SCENE ${number}: ${title} (${duration})\n` +
    `[Scene: ${scene}]\n` +
    `[Visual: ${visual}]\n` +
    `[Tone: ${tone}]\n` +
    `[Camera: ${camera}]\n` +
    `DIALOGUE: ${dialogue}`
  );
}

function stubPunditScript(req: PunditScriptRequest): string {
  const persona = findPersonaBySlug(req.styleSlug);
  const name = persona?.name ?? "The Pundit";
  const match = req.match;
  const home = match?.teamA ?? "Home";
  const away = match?.teamB ?? "Away";
  const isPost = req.phase === "post_match";
  const scoreLine =
    match?.homeScore != null && match?.awayScore != null
      ? `${home} ${match.homeScore}-${match.awayScore} ${away}`
      : `${home} vs ${away}`;
  const venue = match?.venue ?? "the stadium";
  const duration = req.duration ?? 60;

  const scenes = [
    buildScene(
      1,
      "INTRODUCTION",
      "3-5s",
      "studio",
      "team crests split screen",
      "excited",
      "close-up",
      isPost
        ? `${scoreLine}. ${name} here — let's break down what happened.`
        : `${home} against ${away}. ${name}, and this one matters.`
    ),
    buildScene(
      2,
      "MATCH CONTEXT",
      "10-15s",
      "studio",
      `${venue} exterior`,
      "analytical",
      "wide",
      isPost
        ? `Full time at ${venue}. ${match?.stage ?? "The stage"} — this result shifts the narrative.`
        : `We're at ${venue}. ${match?.stage ?? "Big stage"}. Everything on the line tonight.`
    ),
    buildScene(
      3,
      isPost ? "KEY MOMENT" : "MATCHUP TO WATCH",
      "10-15s",
      isPost ? "replay" : "pitch-side",
      isPost ? "slow-motion highlight" : "tactical wide shot",
      "serious",
      "slow-mo",
      isPost
        ? "The moment that flipped the game — that's the clip you rewatch."
        : "Watch the midfield battle — whoever controls tempo wins this."
    ),
    buildScene(
      4,
      "PLAYER PERFORMANCE",
      "10-15s",
      "replay",
      "player close-up",
      "passionate",
      "close-up",
      isPost
        ? "One player stood above the rest — take a bow."
        : "Keep your eye on the key man — if they turn up, it's game over."
    ),
    buildScene(
      5,
      "TACTICAL ANALYSIS",
      "12-18s",
      "studio",
      "tactical board with arrows",
      "analytical",
      "overhead",
      "Shape, pressing, and who commits first — that's the tactical story."
    ),
    buildScene(
      6,
      "TURNING POINT",
      "8-12s",
      "replay",
      "highlight montage",
      "shocked",
      "wide",
      "One moment changed everything — mark it, clip it, debate it."
    ),
    buildScene(
      7,
      "CONCLUSION",
      "8-12s",
      "studio",
      "presenter to camera",
      "serious",
      "close-up",
      isPost
        ? `So where does ${scoreLine} leave us? On to the next one.`
        : "My read? Tight, tense, and the big moments decide it."
    ),
    buildScene(
      8,
      "CLOSING",
      "3-5s",
      "studio",
      "sign-off with channel logo",
      "passionate",
      "close-up",
      persona?.archetype.includes("Silky")
        ? "Football, when played like this — c'est magnifique."
        : "That's the analysis. Now let's see if they listen."
    ),
  ];

  return (
    `--- PUNDIT SCRIPT: ${name} | ${scoreLine} | ${duration}s ${isPost ? "POST-MATCH" : "PRE-MATCH"} ---\n\n` +
    scenes.join("\n\n")
  );
}

async function generatePunditScript(req: PunditScriptRequest): Promise<PunditScriptResult> {
  try {
    const result = await apiFetch<{ content: string; remainingGenerations?: number }>(
      "/api/ai/pundit-script",
      {
        method: "POST",
        body: JSON.stringify({
          matchId: req.matchId,
          phase: req.phase,
          styleSlug: req.styleSlug,
          duration: req.duration ?? 60,
        }),
      }
    );
    return {
      content: result.content,
      type: "pundit_script",
      phase: req.phase,
      styleSlug: req.styleSlug,
      remainingGenerations: result.remainingGenerations,
    };
  } catch (error) {
    if (error instanceof ApiError) {
      return {
        content: stubPunditScript(req),
        type: "pundit_script_stub",
        phase: req.phase,
        styleSlug: req.styleSlug,
      };
    }
    throw error;
  }
}

export function usePunditScript() {
  return useMutation({
    mutationFn: generatePunditScript,
  });
}
