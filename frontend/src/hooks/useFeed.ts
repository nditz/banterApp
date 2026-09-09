"use client";

import { useInfiniteQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { normalizeFeedResponse } from "@/lib/feed";
import type { FeedItem, PaginatedResponse } from "@/lib/types";

const PAGE_SIZE = 5;

async function fetchFeedPage(page: number): Promise<PaginatedResponse<FeedItem>> {
  const response = await apiFetch<unknown>(
    `/api/feed?page=${page}&pageSize=${PAGE_SIZE}`
  );
  return normalizeFeedResponse(response, page, PAGE_SIZE);
}

export function useFeed() {
  return useInfiniteQuery({
    queryKey: ["feed"],
    queryFn: ({ pageParam }) => fetchFeedPage(pageParam),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.hasMore ? lastPage.page + 1 : undefined,
    staleTime: 30_000,
    select: (data) => ({
      ...data,
      feedMode: data.pages[0]?.feedMode,
    }),
  });
}
