import { stripHtml } from "./strip-html";
import {
  FEED_ITEM_TYPES,
  type FeedItem,
  type FeedItemType,
  type FeedMedia,
  type FeedMediaType,
  type FeedReactions,
  type PaginatedResponse,
} from "./types";

type ApiFeedItem = {
  id?: string;
  type?: string;
  title?: string;
  body?: string;
  summary?: string;
  imageUrl?: string;
  media?: {
    type?: string;
    url?: string;
    posterUrl?: string;
    audioUrl?: string;
    alt?: string;
  };
  source?: string;
  sourceUrl?: string;
  url?: string;
  author?: string;
  publishedAt?: string;
  likes?: number;
  viewCount?: number;
  reactions?: {
    agree?: number;
    stale?: number;
    disagree?: number;
  };
  contentLabel?: string;
  receiptId?: string;
  storyId?: string;
  feedItemId?: string;
  matchId?: string;
};

const RECEIPT_LIKE_TYPES = new Set<FeedItemType>([
  "pundit_receipt",
  "user_pundit_compare",
  "community_receipt",
  "studio_story",
  "prediction_highlight",
]);

function isGifUrl(url: string): boolean {
  return (
    /\.gif($|[?#])/i.test(url) ||
    url.includes("giphy.com") ||
    url.includes("tenor.com") ||
    url.startsWith("/reactions/")
  );
}

function mapReactions(raw: ApiFeedItem["reactions"]): FeedReactions | undefined {
  if (!raw) return undefined;
  return {
    agree: raw.agree ?? 0,
    stale: raw.stale ?? 0,
    disagree: raw.disagree ?? 0,
  };
}

function optionalId(value?: string): string | undefined {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}

function optionalPublishedAt(value?: string): string | undefined {
  if (!value) return undefined;
  const time = new Date(value).getTime();
  return Number.isFinite(time) ? value : undefined;
}

function mapFeedItem(raw: ApiFeedItem, index: number): FeedItem | null {
  const id = raw.id ?? `feed-${index}`;
  const title = stripHtml(raw.title);
  const body = stripHtml(raw.body ?? raw.summary);

  if (!title || !body) {
    return null;
  }

  const type = raw.type as FeedItem["type"] | undefined;

  const mediaType = raw.media?.type;
  let media: FeedMedia | undefined =
    raw.media?.url && isFeedMediaType(mediaType)
      ? {
          type: mediaType,
          url: raw.media.url,
          posterUrl: raw.media.posterUrl,
          audioUrl: raw.media.audioUrl,
          alt: raw.media.alt,
        }
      : undefined;

  if (!media && raw.imageUrl) {
    media = {
      type: isGifUrl(raw.imageUrl) ? "gif" : "image",
      url: raw.imageUrl,
      alt: title,
    };
  }

  return {
    id,
    type: type && isFeedItemType(type) ? type : "news",
    title,
    body,
    imageUrl: raw.imageUrl,
    media,
    source: raw.source,
    sourceUrl: raw.sourceUrl ?? raw.url,
    author: raw.author,
    publishedAt: optionalPublishedAt(raw.publishedAt),
    likes: raw.likes ?? raw.viewCount,
    reactions: mapReactions(raw.reactions),
    contentLabel: raw.contentLabel,
    receiptId: optionalId(raw.receiptId),
    storyId: optionalId(raw.storyId),
    feedItemId: optionalId(raw.feedItemId),
    matchId: optionalId(raw.matchId),
  };
}

function isFeedMediaType(value: string | undefined): value is FeedMediaType {
  return value === "image" || value === "gif" || value === "video" || value === "clip";
}

export function isFeedItemType(value: string): value is FeedItem["type"] {
  return (FEED_ITEM_TYPES as readonly string[]).includes(value);
}

/** Relative label for a real publishedAt. Missing/invalid → null (never "Just now"). */
export function formatFeedPublishedAt(
  iso?: string | null
): { label: string; dateTime: string } | null {
  if (!iso) return null;
  const date = new Date(iso);
  const time = date.getTime();
  if (!Number.isFinite(time)) return null;

  const diff = Date.now() - time;
  if (!Number.isFinite(diff) || diff < 0) {
    return { label: "Time unknown", dateTime: date.toISOString() };
  }

  const mins = Math.floor(diff / 60_000);
  if (mins < 60) {
    const shown = Math.max(1, mins);
    return { label: `${shown}m ago`, dateTime: date.toISOString() };
  }

  const hours = Math.floor(mins / 60);
  if (hours < 24) {
    return { label: `${hours}h ago`, dateTime: date.toISOString() };
  }

  return { label: `${Math.floor(hours / 24)}d ago`, dateTime: date.toISOString() };
}

export function isReceiptLikeFeedType(type: FeedItemType): boolean {
  return RECEIPT_LIKE_TYPES.has(type);
}

/** Studio accepts `/studio?receipt=` when the API sent a receipt/story id. Never uses item.id. */
export function studioHrefForFeedItem(item: FeedItem): string | null {
  if (!isReceiptLikeFeedType(item.type)) return null;
  const id = item.receiptId || item.storyId || item.feedItemId;
  if (!id) return null;
  return `/studio?receipt=${encodeURIComponent(id)}`;
}

export function pickHrefForFeedItem(item: FeedItem): string | null {
  return item.matchId ? "/matchweek" : null;
}

export function normalizeFeedResponse(
  response: unknown,
  page: number,
  pageSize: number
): PaginatedResponse<FeedItem> {
  if (Array.isArray(response)) {
    const items = response
      .map((item, index) => mapFeedItem(item as ApiFeedItem, index))
      .filter((item): item is FeedItem => item !== null);

    return {
      items,
      page,
      pageSize,
      totalCount: items.length,
      hasMore: false,
    };
  }

  if (response && typeof response === "object") {
    const payload = response as Record<string, unknown>;
    const rawItems = Array.isArray(payload.items) ? payload.items : [];

    const items = rawItems
      .map((item, index) => mapFeedItem(item as ApiFeedItem, index))
      .filter((item): item is FeedItem => item !== null);

    const totalCount =
      typeof payload.totalCount === "number" ? payload.totalCount : items.length;
    const currentPage = typeof payload.page === "number" ? payload.page : page;
    const size = typeof payload.pageSize === "number" ? payload.pageSize : pageSize;
    const hasMore =
      typeof payload.hasMore === "boolean"
        ? payload.hasMore
        : currentPage * size < totalCount;

    const feedMode =
      payload.feedMode === "personal" || payload.feedMode === "pundit"
        ? payload.feedMode
        : undefined;

    return {
      items,
      page: currentPage,
      pageSize: size,
      totalCount,
      hasMore,
      feedMode,
    };
  }

  const empty: PaginatedResponse<FeedItem> = {
    items: [],
    page,
    pageSize,
    totalCount: 0,
    hasMore: false,
  };
  return empty;
}
