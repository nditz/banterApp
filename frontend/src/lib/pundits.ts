/** Obvious parody — users should recognise the desk, not think it's the real person. */
export const PUNDIT_PARODY_DISCLAIMER =
  "Obvious parody desk — not affiliated with any real person, podcast, or broadcaster.";

/** Chrome fallback when a directory/source label still mentions World Cup. */
export const PREMIER_LEAGUE_SOURCE_LABEL = "Premier League source";

const WORLD_CUP_LABEL = /world\s*cup/i;

/**
 * Strip leftover World Cup wording from pundit directory chrome.
 * Never hides the pundit — only the label. Empty leftovers become a PL-safe source label.
 */
export function sanitizePunditChromeLabel(text?: string | null): string | undefined {
  if (!text?.trim()) return undefined;
  const trimmed = text.trim();
  if (WORLD_CUP_LABEL.test(trimmed)) return PREMIER_LEAGUE_SOURCE_LABEL;
  return trimmed;
}

export function formatPunditSubtitle(entry: {
  parodyCue?: string;
  archetype?: string;
  organization?: string;
}): string | undefined {
  const parodyCue = sanitizePunditChromeLabel(entry.parodyCue);
  if (parodyCue) {
    return parodyCue;
  }
  const archetype = sanitizePunditChromeLabel(entry.archetype);
  const organization = sanitizePunditChromeLabel(entry.organization);
  if (archetype) {
    return organization ? `${archetype} · ${organization}` : archetype;
  }
  return organization;
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
  if (WORLD_CUP_LABEL.test(platform)) {
    return PREMIER_LEAGUE_SOURCE_LABEL;
  }
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
