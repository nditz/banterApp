import { describe, expect, it } from "vitest";
import { formatStudioPackText, STUDIO_CONTENT_TYPES, STUDIO_TONES } from "./studio-pack";
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
  it("lists planned content types and tones", () => {
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
  });

  it("copies sourced facts separately from the creative script", () => {
    const text = formatStudioPackText(pack);
    expect(text).toContain("## Facts (sourced)");
    expect(text).toContain("Result: Arsenal 2–0 Chelsea [match]");
    expect(text).toContain("## Script");
    expect(text).toContain("## AI image prompt");
    expect(text).not.toContain("I bottled the derby");
  });
});
