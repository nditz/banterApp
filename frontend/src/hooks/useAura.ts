"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { AuraSummary } from "@/lib/types";

const EMPTY_AURA: AuraSummary = {
  total: 0,
  weeklyChange: 0,
  streak: 0,
  settledPicks: 0,
  correctPicks: 0,
  rank: null,
  totalPlayers: null,
  percentile: null,
};

/**
 * Aura is the user-facing label for server-awarded points. There is no client-side total:
 * the value only moves when a prediction settles on the server.
 */
export function useAura() {
  const query = useQuery({
    queryKey: ["aura", "me"],
    queryFn: async () => {
      const response = await apiFetch<AuraSummary>("/api/aura/me");
      return response ?? EMPTY_AURA;
    },
    staleTime: 60_000,
  });

  return {
    summary: query.data ?? EMPTY_AURA,
    aura: query.data?.total ?? 0,
    isLoading: query.isLoading,
    isError: query.isError,
  };
}
