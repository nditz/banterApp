import { describe, expect, it } from "vitest";
import {
  formatStudioPackText,
  suggestedStudioPerspective,
  STUDIO_CONTENT_TYPES,
  STUDIO_PERSPECTIVES,
  STUDIO_TONES,
} from "./studio-pack";
import type { StudioContentPack } from "./types";

const pack: StudioContentPack = {
  id: "pack-1",
  contentType: "short",
  tone: "analytical",
  title: "Arsenal vs Chelsea",
  hook: "Arsenal 2–0 Chelsea. You were on Arsenal to win.",
  script: "Recorded result: Arsenal 2–0 Chelsea.",
  facts: [
    { label: "Result", value: "Arsenal 2–0 Chelsea", provenance: "match" },
    { label: "Your pick", value: "Arsenal to win", provenance: "prediction" },
  ],
  visualPlan: ["Scoreline card: Arsenal 2–0 Chelsea."],
  caption: "Arsenal 2–0 Chelsea. Pick: Arsenal to win.",
  hashtags: ["#BallTakes", "#PremierLeague"],
  imagePrompt: "Editorial football graphic, scoreline exactly \"Arsenal 2–0 Chelsea\"",
  voiceoverPrompt: "Read the script. Only mention sourced facts.",
  sourceNotes: ["Facts are copied from your Ball Takes receipt."],
  storyKind: "receipt",
  receiptId: "r1",
  createdAt: "2026-09-09T00:00:00Z",
};

describe("studio pack export", () => {
  it("lists planned content types, tones and perspectives", () => {
    expect(STUDIO_CONTENT_TYPES.map((t) => t.id)).toEqual([
      "short",
      "podcast",
      "meme",
      "caption",
      "thread",
      "carousel",
      "commentary",
    ]);
    expect(STUDIO_TONES.map((t) => t.id)).toContain("victory_lap");
    expect(STUDIO_PERSPECTIVES.map((p) => p.id)).toEqual([
      "me_vs_pundit",
      "my_take",
      "pundit_receipt",
      "match_story",
      "league_story",
    ]);
  });

  it("copies sourced facts separately from the creative script", () => {
    const text = formatStudioPackText(pack);
    expect(text).toContain("## Facts (sourced)");
    expect(text).toContain("Result: Arsenal 2–0 Chelsea [match]");
    expect(text).toContain("## Script (AI-generated)");
    expect(text).toContain("## Hook (AI-generated)");
    expect(text).toContain("## AI image prompt");
    expect(text).toContain("Do not invent quotes");
    expect(text).not.toContain("I bottled the derby");
  });

  it("includes perspective in copied pack text when chosen", () => {
    const text = formatStudioPackText(pack, { perspective: "me_vs_pundit" });
    expect(text).toContain("Perspective: Me vs pundit");
  });

  it("suggests me vs pundit for sourced clash stories", () => {
    expect(suggestedStudioPerspective({ kind: "vs_pundit" })).toBe("me_vs_pundit");
    expect(suggestedStudioPerspective({ kind: "receipt", storyType: "beat_pundit" })).toBe(
      "me_vs_pundit"
    );
    expect(suggestedStudioPerspective({ kind: "trending" })).toBe("match_story");
    expect(suggestedStudioPerspective({ kind: "receipt", storyType: "hit" })).toBe("my_take");
  });
});
