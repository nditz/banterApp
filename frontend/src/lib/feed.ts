import type { FeedItem, FeedMedia, FeedMediaType, PaginatedResponse } from "./types";

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
};

function mapFeedItem(raw: ApiFeedItem, index: number): FeedItem | null {
  const id = raw.id ?? `feed-${index}`;
  const title = raw.title?.trim();
  const body = (raw.body ?? raw.summary)?.trim();

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
      type: "image",
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
    publishedAt: raw.publishedAt ?? new Date().toISOString(),
    likes: raw.likes ?? raw.viewCount,
  };
}

function isFeedMediaType(value: string | undefined): value is FeedMediaType {
  return value === "image" || value === "gif" || value === "video" || value === "clip";
}

function isFeedItemType(value: string): value is FeedItem["type"] {
  return [
    "banter",
    "meme",
    "news",
    "leaderboard",
    "prediction_highlight",
    "pundit_quote",
  ].includes(value);
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
