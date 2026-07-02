"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";
import { mockMatches } from "@/lib/mock-data";
import type { Match } from "@/lib/types";

async function fetchUpcomingMatches(): Promise<Match[]> {
  try {
    return await apiFetch<Match[]>("/api/matches/upcoming");
  } catch (error) {
    if (error instanceof ApiError) {
      return mockMatches;
    }
    throw error;
  }
}

export function useMatches() {
  return useQuery({
    queryKey: ["matches", "upcoming"],
    queryFn: fetchUpcomingMatches,
    staleTime: 60_000,
  });
}

async function fetchMatchResults(): Promise<Match[]> {
  try {
    return await apiFetch<Match[]>("/api/matches/results");
  } catch (error) {
    if (error instanceof ApiError) {
      return mockMatches.filter(
        (m) => m.homeScore != null && m.awayScore != null
      );
    }
    throw error;
  }
}

export function useMatchResults() {
  return useQuery({
    queryKey: ["matches", "results"],
    queryFn: fetchMatchResults,
    staleTime: 60_000,
  });
}

export function useMatch(matchId: string) {
  return useQuery({
    queryKey: ["matches", matchId],
    queryFn: async () => {
      try {
        return await apiFetch<Match>(`/api/matches/${matchId}`);
      } catch {
        return mockMatches.find((m) => m.id === matchId) ?? null;
      }
    },
    enabled: Boolean(matchId),
  });
}
