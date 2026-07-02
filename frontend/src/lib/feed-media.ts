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

/** Default media when feed items arrive without images. */
export const DEFAULT_FEED_MEDIA: Partial<Record<FeedItemType, FeedMedia>> = {
  banter: {
    type: "gif",
    url: BANTER_REACTION_GIFS.against_grain!,
    alt: "Football banter reaction",
  },
  meme: {
    type: "gif",
    url: "/reactions/chaos-pick.svg",
    alt: "Meme reaction",
  },
  leaderboard: {
    type: "image",
    url: "https://images.unsplash.com/photo-1574629810360-7efbbe195018?w=640&h=360&fit=crop",
    alt: "Fans celebrating in the stands",
  },
  prediction_highlight: {
    type: "image",
    url: "https://images.unsplash.com/photo-1431324155629-1a6deb1dec8d?w=640&h=360&fit=crop",
    alt: "Goal celebration",
  },
  news: {
    type: "image",
    url: "https://images.unsplash.com/photo-1522778119026-d647f0596c20?w=640&h=360&fit=crop",
    alt: "Football news",
  },
};

export function getBanterMediaForReaction(
  reactionKey: ReactionKey,
  assetUrl: string
): FeedMedia {
  const gif = BANTER_REACTION_GIFS[reactionKey];
  if (gif) {
    return { type: "gif", url: gif, alt: "Banter reaction" };
  }

  return { type: "image", url: assetUrl, alt: "Banter reaction" };
}

/** Local sticker used to replace any retired external Giphy URL. */
const GIPHY_FALLBACK_STICKER = "/reactions/against-grain.svg";

/**
 * Neutralizes retired Giphy links (which 404) that may still live on persisted feed items,
 * so the browser never requests a dead URL. Returns a local sticker in their place.
 */
export function sanitizeMediaUrl(url: string): string {
  return /giphy\.com/i.test(url) ? GIPHY_FALLBACK_STICKER : url;
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
