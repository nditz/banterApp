import { PRODUCT_METRICS, recordMetric } from "@/lib/metrics";
import type { StudioContentPack, StudioContentType, StudioTone } from "@/lib/types";

export const STUDIO_CONTENT_TYPES: { id: StudioContentType; label: string }[] = [
  { id: "short", label: "Short / Reel" },
  { id: "podcast", label: "Podcast" },
  { id: "meme", label: "Meme" },
  { id: "caption", label: "Caption" },
  { id: "thread", label: "Thread" },
  { id: "carousel", label: "Carousel" },
  { id: "commentary", label: "Commentary" },
];

export const STUDIO_TONES: { id: StudioTone; label: string }[] = [
  { id: "funny", label: "Funny" },
  { id: "ruthless", label: "Ruthless" },
  { id: "analytical", label: "Analytical" },
  { id: "rant", label: "Rant" },
  { id: "victory_lap", label: "Victory lap" },
  { id: "self_roast", label: "Self roast" },
  { id: "pundit", label: "Pundit style" },
  { id: "explainer", label: "Explainer" },
];

export function formatStudioPackText(pack: StudioContentPack): string {
  const facts = pack.facts.map((f) => `- ${f.label}: ${f.value} [${f.provenance}]`).join("\n");
  const visuals = pack.visualPlan.map((v) => `- ${v}`).join("\n");
  const notes = pack.sourceNotes.map((n) => `- ${n}`).join("\n");
  return [
    `# ${pack.title}`,
    `Type: ${pack.contentType} · Tone: ${pack.tone}`,
    "",
    "## Hook",
    pack.hook,
    "",
    "## Script",
    pack.script,
    "",
    "## Facts (sourced)",
    facts,
    "",
    "## Visual plan",
    visuals,
    pack.memeDirection ? `\n## Meme / GIF direction\n${pack.memeDirection}` : "",
    "",
    "## Caption",
    pack.caption,
    "",
    "## Hashtags",
    pack.hashtags.join(" "),
    "",
    "## AI image prompt",
    pack.imagePrompt,
    "",
    "## Voiceover prompt",
    pack.voiceoverPrompt,
    "",
    "## Source notes",
    notes,
  ]
    .filter((block) => block !== "")
    .join("\n");
}

/** Downloads a generated pack and records the export as a product funnel event. */
export function downloadTextFile(filename: string, contents: string, mime = "text/plain") {
  recordMetric(PRODUCT_METRICS.contentExported);
  const blob = new Blob([contents], { type: mime });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}
