"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { FootballCountry, FootballPlayer, LeaderboardEntry } from "@/lib/football-reference/types";

function dedupeCountries(countries: FootballCountry[]): FootballCountry[] {
  const byCode = new Map<string, FootballCountry>();
  for (const country of countries) {
    const key = (country.code ?? country.id).trim().toUpperCase();
    if (!key || byCode.has(key)) continue;
    byCode.set(key, country);
  }
  return Array.from(byCode.values()).sort((a, b) =>
    a.name.localeCompare(b.name, undefined, { sensitivity: "base" })
  );
}

const key = ["football-reference"] as const;

export function useFootballCountries(search?: string) {
  const params = new URLSearchParams();
  if (search) params.set("search", search);
  const qs = params.toString();

  return useQuery({
    queryKey: [...key, "countries", search ?? ""],
    queryFn: () =>
      apiFetch<{ countries: FootballCountry[] }>(
        `/api/football/countries${qs ? `?${qs}` : ""}`
      ).then((r) => dedupeCountries(r.countries)),
  });
}

export function useFootballPlayers(filters?: {
  countryId?: string;
  search?: string;
  position?: string;
  limit?: number;
}) {
  const params = new URLSearchParams();
  if (filters?.countryId) params.set("countryId", filters.countryId);
  if (filters?.search) params.set("search", filters.search);
  if (filters?.position) params.set("position", filters.position);
  if (filters?.limit) params.set("limit", String(filters.limit));
  const qs = params.toString();

  return useQuery({
    queryKey: [...key, "players", filters ?? {}],
    queryFn: () =>
      apiFetch<{ players: FootballPlayer[] }>(
        `/api/football/players${qs ? `?${qs}` : ""}`
      ).then((r) => r.players),
  });
}

export function useTopScorersLeaderboard() {
  return useQuery({
    queryKey: [...key, "leaderboard", "top-scorers"],
    queryFn: () =>
      apiFetch<{ entries: LeaderboardEntry[] }>(
        "/api/football/leaderboards/top-scorers"
      ).then((r) => r.entries),
  });
}

export function useTopAssistsLeaderboard() {
  return useQuery({
    queryKey: [...key, "leaderboard", "top-assists"],
    queryFn: () =>
      apiFetch<{ entries: LeaderboardEntry[] }>(
        "/api/football/leaderboards/top-assists"
      ).then((r) => r.entries),
  });
}
