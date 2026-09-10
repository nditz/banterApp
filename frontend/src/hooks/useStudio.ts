"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { StudioComparison } from "@/lib/types";

export function useStudio(matchIds?: string[]) {
  const key = matchIds?.length ? [...matchIds].sort().join(",") : "mine";
  return useQuery<StudioComparison>({
    queryKey: ["studio", "comparison", key],
    enabled: matchIds === undefined || matchIds.length > 0,
    queryFn: async () => {
      const qs = matchIds?.length
        ? `?matchIds=${encodeURIComponent(matchIds.join(","))}`
        : "";
      return await apiFetch<StudioComparison>(`/api/studio/comparison${qs}`);
    },
    staleTime: 30_000,
  });
}
