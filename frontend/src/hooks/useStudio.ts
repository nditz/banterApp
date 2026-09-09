"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";
import type { StudioComparison } from "@/lib/types";

const EMPTY_COMPARISON: StudioComparison = {
  myTotalPoints: 0,
  myLeagueRank: undefined,
  leagueTotal: undefined,
  matches: [],
  followedPunditCount: 0,
  filteringToFollows: false,
};

export function useStudio(matchIds?: string[]) {
  const key = matchIds?.length ? [...matchIds].sort().join(",") : "mine";
  return useQuery<StudioComparison>({
    queryKey: ["studio", "comparison", key],
    enabled: matchIds === undefined || matchIds.length > 0,
    queryFn: async () => {
      try {
        const qs = matchIds?.length
          ? `?matchIds=${encodeURIComponent(matchIds.join(","))}`
          : "";
        return await apiFetch<StudioComparison>(`/api/studio/comparison${qs}`);
      } catch (e) {
        if (e instanceof ApiError) return EMPTY_COMPARISON;
        throw e;
      }
    },
    staleTime: 30_000,
  });
}
