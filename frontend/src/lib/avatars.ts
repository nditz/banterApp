import { getInitials } from "@/lib/leaderboard";
import type { League, LeagueKind } from "@/lib/types";

const AVATAR_COLORS = [
  "1a472a",
  "14532d",
  "0f766e",
  "166534",
  "1e3a5f",
  "7c2d12",
] as const;

function hashSeed(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

function pickColor(seed: string): string {
  return AVATAR_COLORS[hashSeed(seed) % AVATAR_COLORS.length];
}

/** Deterministic default avatar for players and pundits (FPL-style manager photo). */
export function getUserAvatarUrl(
  userId: string,
  displayName: string,
  avatarUrl?: string
): string | undefined {
  if (avatarUrl?.trim()) {
    return avatarUrl.trim();
  }

  const seed = encodeURIComponent(userId || displayName || "player");
  const color = pickColor(userId || displayName);
  return `https://api.dicebear.com/9.x/thumbs/svg?seed=${seed}&backgroundColor=${color}`;
}

/** Default avatar for leagues — country flag, global fans shot, or custom crest. */
export function getLeagueAvatarUrl(league: {
  id: string;
  name: string;
  kind?: LeagueKind;
  countryCode?: string;
}): string {
  if (league.kind === "global") {
    return "/images/fans_main_image_1.png";
  }

  if (league.kind === "country" && league.countryCode?.trim()) {
    return `https://flagcdn.com/w80/${league.countryCode.trim().toLowerCase()}.png`;
  }

  const seed = encodeURIComponent(league.id || league.name);
  const color = pickColor(league.name);
  return `https://api.dicebear.com/9.x/shapes/svg?seed=${seed}&backgroundColor=${color},fbbf24,d97706`;
}

export function getAvatarInitials(displayName: string): string {
  return getInitials(displayName);
}
