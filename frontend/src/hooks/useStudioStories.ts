"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { StudioStories } from "@/lib/types";

export function useStudioStories() {
  return useQuery<StudioStories>({
    queryKey: ["studio", "stories"],
    queryFn: () => apiFetch<StudioStories>("/api/studio/stories"),
    staleTime: 30_000,
    retry: 1,
  });
}
