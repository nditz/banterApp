"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";
import type { LeagueTableRow } from "@/lib/league-table";
import { mockMatches } from "@/lib/mock-data";
import type { Match } from "@/lib/types";

async function fetchUpcomingMatches(): Promise<Match[]> {
  try {
    return await apiFetch<Match[]>("/api/matches/upcoming");
  } catch (error) {
    if (error instanceof ApiError) {
      return mockMatches.filter((m) => m.status !== "FT");
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
      return mockMatches.filter((m) => m.homeScore != null && m.awayScore != null);
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

export function useCurrentMatchweek() {
  return useQuery({
    queryKey: ["matchweeks", "current"],
    queryFn: async () => {
      try {
        return await apiFetch<{ number: number; matches: Match[] }>("/api/matchweeks/current");
      } catch (error) {
        if (error instanceof ApiError) {
          const openWeeks = mockMatches
            .filter((m) => m.status !== "FT" && m.matchweekNumber)
            .map((m) => m.matchweekNumber as number);
          const number = openWeeks.length > 0 ? Math.min(...openWeeks) : 1;
          return {
            number,
            matches: mockMatches.filter((m) => m.matchweekNumber === number),
          };
        }
        throw error;
      }
    },
    staleTime: 30_000,
  });
}

export function useLeagueTable() {
  return useQuery({
    queryKey: ["standings"],
    queryFn: async () => {
      try {
        return await apiFetch<LeagueTableRow[]>("/api/standings");
      } catch {
        return [];
      }
    },
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
