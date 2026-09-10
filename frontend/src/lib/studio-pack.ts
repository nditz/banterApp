import { PRODUCT_METRICS, recordMetric } from "@/lib/metrics";
import type {
  StudioContentPack,
  StudioContentType,
  StudioPerspective,
  StudioStoryKind,
  StudioTone,
} from "@/lib/types";

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

export const STUDIO_PERSPECTIVES: {
  id: StudioPerspective;
  label: string;
  hint: string;
}[] = [
  {
    id: "me_vs_pundit",
    label: "Me vs pundit",
    hint: "Your pick against a sourced desk",
  },
  {
    id: "my_take",
    label: "My take",
    hint: "Your prediction, in your voice",
  },
  {
    id: "pundit_receipt",
    label: "Pundit receipt",
    hint: "A sourced call that landed or missed",
  },
  {
    id: "match_story",
    label: "Match story",
    hint: "What happened on the pitch",
  },
  {
    id: "league_story",
    label: "League story",
    hint: "How you stack up with friends",
  },
];

export function studioPerspectiveLabel(id?: StudioPerspective | null): string | null {
  if (!id) return null;
  return STUDIO_PERSPECTIVES.find((item) => item.id === id)?.label ?? id;
}

export function suggestedStudioPerspective(story: {
  kind: StudioStoryKind | string;
  storyType?: string | null;
}): StudioPerspective {
  const storyType = story.storyType?.toLowerCase() ?? "";
  if (
    story.kind === "vs_pundit" ||
    storyType === "beat_pundit" ||
    storyType === "pundit_beat_user"
  ) {
    return "me_vs_pundit";
  }
  if (story.kind === "trending") return "match_story";
  return "my_take";
}

export function formatStudioPackText(
  pack: StudioContentPack,
  options?: { perspective?: StudioPerspective | null }
): string {
  const facts = pack.facts.map((f) => `- ${f.label}: ${f.value} [${f.provenance}]`).join("\n");
  const visuals = pack.visualPlan.map((v) => `- ${v}`).join("\n");
  const notes = pack.sourceNotes.map((n) => `- ${n}`).join("\n");
  const perspective = studioPerspectiveLabel(options?.perspective);
  const typeLine = [
    `Type: ${pack.contentType}`,
    `Tone: ${pack.tone}`,
    perspective ? `Perspective: ${perspective}` : null,
  ]
    .filter(Boolean)
    .join(" · ");
  return [
    `# ${pack.title}`,
    typeLine,
    "",
    "## Attribution",
    "- Hook, script, caption and prompts are AI-generated from locked facts.",
    "- Facts and pundit lines come from the receipt or sourced headline. Do not invent quotes.",
    "",
    "## Hook (AI-generated)",
    pack.hook,
    "",
    "## Script (AI-generated)",
    pack.script,
    "",
    "## Facts (sourced)",
    facts,
    "",
    "## Visual plan",
    visuals,
    pack.memeDirection ? `\n## Meme / GIF direction\n${pack.memeDirection}` : "",
    "",
    "## Caption (AI-generated)",
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
