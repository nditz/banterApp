import { afterEach, describe, expect, it, vi } from "vitest";
import {
  formatFeedPublishedAt,
  isFeedItemType,
  normalizeFeedResponse,
  studioHrefForFeedItem,
} from "./feed";
import type { FeedItem } from "./types";

function item(overrides: Partial<FeedItem> & Pick<FeedItem, "type">): FeedItem {
  return {
    id: "x",
    title: "Title",
    body: "Body",
    ...overrides,
  };
}

describe("isFeedItemType", () => {
  it("accepts existing and new Phase 6 types", () => {
    expect(isFeedItemType("banter")).toBe(true);
    expect(isFeedItemType("pundit_quote")).toBe(true);
    expect(isFeedItemType("gif_reaction")).toBe(true);
    expect(isFeedItemType("pundit_receipt")).toBe(true);
    expect(isFeedItemType("user_pundit_compare")).toBe(true);
    expect(isFeedItemType("community_receipt")).toBe(true);
    expect(isFeedItemType("trending_take")).toBe(true);
    expect(isFeedItemType("match_event")).toBe(true);
    expect(isFeedItemType("studio_story")).toBe(true);
  });

  it("rejects unknown types", () => {
    expect(isFeedItemType("exact_score")).toBe(false);
    expect(isFeedItemType("")).toBe(false);
  });
});

describe("normalizeFeedResponse", () => {
  it("leaves publishedAt optional instead of inventing now", () => {
    const page = normalizeFeedResponse(
      [{ id: "1", type: "news", title: "Hello", body: "World" }],
      1,
      5
    );
    expect(page.items[0]?.publishedAt).toBeUndefined();
  });

  it("keeps a real publishedAt and maps optional ids", () => {
    const page = normalizeFeedResponse(
      [
        {
          id: "2",
          type: "pundit_receipt",
          title: "Take",
          body: "Said it",
          publishedAt: "2026-04-01T12:00:00.000Z",
          receiptId: " rec-1 ",
          matchId: "m-9",
        },
      ],
      1,
      5
    );
    expect(page.items[0]?.type).toBe("pundit_receipt");
    expect(page.items[0]?.publishedAt).toBe("2026-04-01T12:00:00.000Z");
    expect(page.items[0]?.receiptId).toBe("rec-1");
    expect(page.items[0]?.matchId).toBe("m-9");
  });

  it("maps unknown types to news fallback without fabricating copy", () => {
    const page = normalizeFeedResponse(
      [{ id: "3", type: "not_a_real_type", title: "Only this", body: "And this" }],
      1,
      5
    );
    expect(page.items[0]?.type).toBe("news");
    expect(page.items[0]?.title).toBe("Only this");
    expect(page.items[0]?.body).toBe("And this");
  });

  it("still strips HTML from title and body", () => {
    const page = normalizeFeedResponse(
      [{ id: "4", type: "banter", title: "<b>Hi</b>", body: "<p>There</p>" }],
      1,
      5
    );
    expect(page.items[0]?.title).toBe("Hi");
    expect(page.items[0]?.body).toBe("There");
  });
});

describe("formatFeedPublishedAt", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns null for missing or invalid dates", () => {
    expect(formatFeedPublishedAt(undefined)).toBeNull();
    expect(formatFeedPublishedAt("")).toBeNull();
    expect(formatFeedPublishedAt("not-a-date")).toBeNull();
  });

  it("uses Xm ago for a real timestamp under an hour, never Just now", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-09-10T12:00:00.000Z"));
    const recent = formatFeedPublishedAt("2026-09-10T11:40:00.000Z");
    expect(recent?.label).toBe("20m ago");
    expect(recent?.label).not.toBe("Just now");
    const veryRecent = formatFeedPublishedAt("2026-09-10T11:59:30.000Z");
    expect(veryRecent?.label).toBe("1m ago");
  });
});

describe("studioHrefForFeedItem", () => {
  it("builds a Studio receipt link only when a receipt-like id exists", () => {
    expect(
      studioHrefForFeedItem(item({ type: "pundit_receipt", receiptId: "abc" }))
    ).toBe("/studio?receipt=abc");
    expect(studioHrefForFeedItem(item({ type: "pundit_receipt" }))).toBeNull();
    expect(
      studioHrefForFeedItem(item({ type: "news", receiptId: "abc" }))
    ).toBeNull();
  });
});
