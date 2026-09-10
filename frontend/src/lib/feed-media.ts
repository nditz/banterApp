import type { FeedItemType, FeedMedia } from "@/lib/types";
import type { ReactionKey } from "@/reactions/reactionContent";

/**
 * Reaction stickers for banter feed items — used when API items lack media.
 * These are bundled local assets (served from `/public/reactions`) rather than external
 * Giphy links, which 404 once the upstream media IDs rot.
 */
export const BANTER_REACTION_GIFS: Partial<Record<ReactionKey, string>> = {
  smart_choice: "/reactions/smart-choice.svg",
  playing_safe: "/reactions/playing-safe.svg",
  against_grain: "/reactions/against-grain.svg",
  chaos_pick: "/reactions/chaos-pick.svg",
  locked_in: "/reactions/locked-in.svg",
  delulu_vision: "/reactions/delulu-vision.svg",
  receipts_found: "/reactions/receipts-found.svg",
  prediction_fraud: "/reactions/prediction-fraud.svg",
  brave_but_wrong: "/reactions/brave-but-wrong.svg",
  script_writer: "/reactions/script-writer.svg",
};

/**
 * Decorative local stickers only — never stock photography.
 * Types without an entry render a designed CSS placeholder instead.
 */
export const DEFAULT_FEED_MEDIA: Partial<Record<FeedItemType, FeedMedia>> = {
  banter: {
    type: "gif",
    url: BANTER_REACTION_GIFS.against_grain!,
    alt: "",
  },
  meme: {
    type: "gif",
    url: "/reactions/chaos-pick.svg",
    alt: "",
  },
  gif_reaction: {
    type: "gif",
    url: "/reactions/locked-in.svg",
    alt: "",
  },
};

export function getBanterMediaForReaction(
  reactionKey: ReactionKey,
  assetUrl: string
): FeedMedia {
  const gif = BANTER_REACTION_GIFS[reactionKey];
  if (gif) {
    return { type: "gif", url: gif, alt: "" };
  }

  return { type: "image", url: assetUrl, alt: "" };
}

/** Pass-through. Dead remote GIFs are handled by <img onError> in FeedMedia. */
export function sanitizeMediaUrl(url: string): string {
  return url;
}

const NEXT_IMAGE_HOSTS = new Set([
  "flagcdn.com",
  "api.dicebear.com",
  "media.api-sports.io",
  "lh3.googleusercontent.com",
]);

const ANIMATED_OR_VECTOR = /\.(gif|svg)($|[?#])/i;

/**
 * next/image only for hosts in next.config remotePatterns (or same-origin raster).
 * Giphy/Tenor/SVG/GIF stay on `<img>` so we do not break remote config or freeze GIFs.
 */
export function canUseNextImage(url: string): boolean {
  if (!url || ANIMATED_OR_VECTOR.test(url)) return false;
  if (url.startsWith("/") && !url.startsWith("//")) {
    return true;
  }
  try {
    const parsed = new URL(url);
    if (parsed.protocol !== "https:") return false;
    const host = parsed.hostname;
    if (host.endsWith(".googleusercontent.com") || host.endsWith(".supabase.co")) {
      return true;
    }
    return NEXT_IMAGE_HOSTS.has(host);
  } catch {
    return false;
  }
}

export function resolveFeedMedia(item: {
  type: FeedItemType;
  title: string;
  imageUrl?: string;
  media?: FeedMedia;
}): FeedMedia | undefined {
  if (item.media?.url) {
    return { ...item.media, url: sanitizeMediaUrl(item.media.url) };
  }

  if (item.imageUrl) {
    return { type: "image", url: sanitizeMediaUrl(item.imageUrl), alt: item.title };
  }

  return DEFAULT_FEED_MEDIA[item.type];
}
