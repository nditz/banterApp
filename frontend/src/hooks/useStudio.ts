"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";
import type { StudioComparison } from "@/lib/types";

const EMPTY_COMPARISON: StudioComparison = {
  myTotalPoints: 0,
  myLeagueRank: undefined,
  leagueTotal: undefined,
  matches: [],
};

export function useStudio() {
  return useQuery<StudioComparison>({
    queryKey: ["studio", "comparison"],
    queryFn: async () => {
      try {
        return await apiFetch<StudioComparison>("/api/studio/comparison");
      } catch (e) {
        if (e instanceof ApiError) return EMPTY_COMPARISON;
        throw e;
      }
    },
    staleTime: 30_000,
  });
}
