/** Obvious parody — users should recognise the desk, not think it's the real person. */
export const PUNDIT_PARODY_DISCLAIMER =
  "Obvious parody desk — not affiliated with any real person, podcast, or broadcaster.";

export function formatPunditSubtitle(entry: {
  parodyCue?: string;
  archetype?: string;
  organization?: string;
}): string | undefined {
  if (entry.parodyCue?.trim()) {
    return entry.parodyCue.trim();
  }
  if (entry.archetype?.trim()) {
    return entry.organization
      ? `${entry.archetype} · ${entry.organization}`
      : entry.archetype;
  }
  return entry.organization;
}

export function getPunditAvatarUrl(avatarSeed?: string, displayName?: string): string {
  const seed = encodeURIComponent(avatarSeed || displayName || "pundit-desk");
  return `https://api.dicebear.com/9.x/bottts/svg?seed=${seed}&backgroundColor=1a472a,fbbf24,7c2d12`;
}

/** Future podcast / YouTube ingest will tag takes with these platform ids. */
export const PUNDIT_SOURCE_PLATFORMS = [
  "podcast",
  "youtube",
  "article",
  "tv",
  "social",
] as const;

export type PunditSourcePlatform = (typeof PUNDIT_SOURCE_PLATFORMS)[number];

export function formatSourcePlatformLabel(platform?: string): string | undefined {
  if (!platform) return undefined;
  switch (platform.toLowerCase()) {
    case "youtube":
      return "YouTube";
    case "podcast":
      return "Podcast";
    case "article":
      return "Article";
    case "tv":
      return "TV";
    case "social":
      return "Social";
    default:
      return platform;
  }
}
