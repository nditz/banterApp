"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import {
  parseStandingsPayload,
  type CurrentMatchweekPayload,
} from "@/lib/football-dataset";
import type { Match } from "@/lib/types";

export function useMatches() {
  return useQuery({
    queryKey: ["matches", "upcoming"],
    queryFn: () => apiFetch<Match[]>("/api/matches/upcoming"),
    staleTime: 60_000,
  });
}

export function useMatchResults() {
  return useQuery({
    queryKey: ["matches", "results"],
    queryFn: () => apiFetch<Match[]>("/api/matches/results"),
    staleTime: 60_000,
  });
}

export function useCurrentMatchweek() {
  return useQuery({
    queryKey: ["matchweeks", "current"],
    queryFn: () => apiFetch<CurrentMatchweekPayload>("/api/matchweeks/current"),
    staleTime: 30_000,
  });
}

export function useLeagueTable() {
  return useQuery({
    queryKey: ["standings"],
    queryFn: async () => parseStandingsPayload(await apiFetch<unknown>("/api/standings")),
    staleTime: 60_000,
  });
}

export function useMatch(matchId: string) {
  return useQuery({
    queryKey: ["matches", matchId],
    queryFn: () => apiFetch<Match>(`/api/matches/${matchId}`),
    enabled: Boolean(matchId),
  });
}
