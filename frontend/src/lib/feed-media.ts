import type { FeedItemType, FeedMedia } from "@/lib/types";
import type { ReactionKey } from "@/reactions/reactionContent";

/** Reaction GIFs for banter feed items — used when API items lack media. */
export const BANTER_REACTION_GIFS: Partial<Record<ReactionKey, string>> = {
  smart_choice: "https://media.giphy.com/media/3o7TKSjRrfIPjeiVy/giphy.gif",
  playing_safe: "https://media.giphy.com/media/l0MYt5jPR6QX5pnqM/giphy.gif",
  against_grain: "https://media.giphy.com/media/26BRuo6sGiljlMz4s/giphy.gif",
  chaos_pick: "https://media.giphy.com/media/3o6Zt481isNVkbQIhr/giphy.gif",
  locked_in: "https://media.giphy.com/media/l0HlBO7eyXzSZkJri/giphy.gif",
  delulu_vision: "https://media.giphy.com/media/3o7aD2saQq3B5iyTFS/giphy.gif",
  receipts_found: "https://media.giphy.com/media/26gsjCZpPolPr3sBy/giphy.gif",
  prediction_fraud: "https://media.giphy.com/media/ISOckXU5oKAE/giphy.gif",
  brave_but_wrong: "https://media.giphy.com/media/3o6Zt8rCfNXzYvNj2E/giphy.gif",
  script_writer: "https://media.giphy.com/media/3o6Zt6MLCHB0UiZ48I/giphy.gif",
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
    url: "https://media.giphy.com/media/l0MYt5jPR6QX5pnqM/giphy.gif",
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

export function resolveFeedMedia(item: {
  type: FeedItemType;
  title: string;
  imageUrl?: string;
  media?: FeedMedia;
}): FeedMedia | undefined {
  if (item.media?.url) {
    return item.media;
  }

  if (item.imageUrl) {
    return { type: "image", url: item.imageUrl, alt: item.title };
  }

  return DEFAULT_FEED_MEDIA[item.type];
}
