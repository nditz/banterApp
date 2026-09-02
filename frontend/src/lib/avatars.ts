import { getInitials } from "@/lib/leaderboard";
import { isSafeExternalUrl } from "@/lib/safe-url";
import type { LeagueKind } from "@/lib/types";

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

function firstHttpsUrl(...values: unknown[]): string | undefined {
  for (const value of values) {
    if (typeof value === "string" && isSafeExternalUrl(value)) {
      return value.trim();
    }
  }
  return undefined;
}

/** Google OAuth stores the photo on user_metadata.picture / avatar_url. */
export function getSupabaseAvatarUrl(user: {
  user_metadata?: Record<string, unknown> | null;
  identities?: Array<{ identity_data?: Record<string, unknown> | null }> | null;
}): string | undefined {
  const meta = user.user_metadata ?? {};
  const identity = user.identities?.[0]?.identity_data ?? {};
  return firstHttpsUrl(
    meta.avatar_url,
    meta.picture,
    identity.avatar_url,
    identity.picture
  );
}

export function getSupabaseDisplayName(
  user: {
    email?: string | null;
    user_metadata?: Record<string, unknown> | null;
  },
  fallback = "Player"
): string {
  const meta = user.user_metadata ?? {};
  const fromMeta = [meta.full_name, meta.display_name, meta.name].find(
    (value) => typeof value === "string" && value.trim()
  );
  if (typeof fromMeta === "string") return fromMeta.trim();
  const local = user.email?.split("@")[0]?.trim();
  return local || fallback;
}

/** Deterministic default avatar for players and pundits (FPL-style manager photo). */
export function getUserAvatarUrl(
  userId: string,
  displayName: string,
  avatarUrl?: string
): string | undefined {
  if (avatarUrl?.trim()) {
    const trimmed = avatarUrl.trim();
    if (
      trimmed.startsWith("data:image/") ||
      trimmed.startsWith("blob:") ||
      isSafeExternalUrl(trimmed)
    ) {
      return trimmed;
    }
  }

  const seed = encodeURIComponent(userId || displayName || "player");
  const color = pickColor(userId || displayName);
  return `https://api.dicebear.com/9.x/thumbs/svg?seed=${seed}&backgroundColor=${color}`;
}

/** Default avatar for leagues — generated mark, country flag, or custom crest. */
export function getLeagueAvatarUrl(league: {
  id: string;
  name: string;
  kind?: LeagueKind;
  countryCode?: string;
}): string {
  if (league.kind === "global") {
    const seed = encodeURIComponent(league.id || league.name || "global");
    return `https://api.dicebear.com/9.x/shapes/svg?seed=${seed}&backgroundColor=1c1e24,2a2d33`;
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
