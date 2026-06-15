"use client";

import { useInfiniteQuery } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";
import { normalizeFeedResponse } from "@/lib/feed";
import { mockFeedItems } from "@/lib/mock-data";
import type { FeedItem, PaginatedResponse } from "@/lib/types";

const PAGE_SIZE = 5;

function mockFeedPage(page: number): PaginatedResponse<FeedItem> {
  const start = (page - 1) * PAGE_SIZE;
  const items = mockFeedItems.slice(start, start + PAGE_SIZE);
  return {
    items,
    page,
    pageSize: PAGE_SIZE,
    totalCount: mockFeedItems.length,
    hasMore: start + PAGE_SIZE < mockFeedItems.length,
  };
}

async function fetchFeedPage(page: number): Promise<PaginatedResponse<FeedItem>> {
  try {
    const response = await apiFetch<unknown>(
      `/api/feed?page=${page}&pageSize=${PAGE_SIZE}`
    );
    return normalizeFeedResponse(response, page, PAGE_SIZE);
  } catch (error) {
    if (error instanceof ApiError) {
      return mockFeedPage(page);
    }
    throw error;
  }
}

export function useFeed() {
  return useInfiniteQuery({
    queryKey: ["feed"],
    queryFn: ({ pageParam }) => fetchFeedPage(pageParam),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.hasMore ? lastPage.page + 1 : undefined,
    staleTime: 30_000,
  });
}
