"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { dedupeTeamsByCode } from "@/lib/dedupe-teams";
import { getTurnstileToken } from "@/lib/turnstile-token";

export type TournamentBonusCategoryId =
  | "player_of_tournament"
  | "top_scorer"
  | "top_assist"
  | "golden_glove"
  | "surprise_package";

export interface TournamentBonusPick {
  id: string;
  category: TournamentBonusCategoryId;
  pickValue: string;
  pointsAwarded: number;
  lockedAt: string | null;
  createdAt: string;
}

export interface TournamentBonusAward {
  category: TournamentBonusCategoryId;
  answerValue: string;
  answerDisplay: string | null;
  announcedAt: string;
}

export interface TournamentBonusCategoryInfo {
  category: TournamentBonusCategoryId;
  label: string;
  description: string;
  points: number;
  isTeamPick: boolean;
  pick: TournamentBonusPick | null;
  officialResult: TournamentBonusAward | null;
}

export interface TournamentBonusTeam {
  code: string;
  name: string;
}

export interface TournamentBonusStatus {
  isEligible: boolean;
  hasActivity: boolean;
  hasQualifyingLeague: boolean;
  ineligibilityReasons: string[];
  isLocked: boolean;
  canPick: boolean;
  categories: TournamentBonusCategoryInfo[];
  teams: TournamentBonusTeam[];
  playerSuggestions: string[];
}

export function useTournamentBonuses() {
  return useQuery({
    queryKey: ["tournament-bonuses"],
    queryFn: async () => {
      const status = await apiFetch<TournamentBonusStatus>("/api/tournament-bonuses");
      return { ...status, teams: dedupeTeamsByCode(status.teams) };
    },
    retry: 1,
  });
}

export interface TournamentBonusPlayerOption {
  name: string;
  teamCode: string;
  teamName: string;
}

interface PlayerSearchResponse {
  players: TournamentBonusPlayerOption[];
}

export function usePlayerSearch(
  query: string,
  teamCode: string | null,
  enabled: boolean
) {
  return useQuery({
    queryKey: ["tournament-bonus-players", query.trim().toLowerCase(), teamCode ?? ""],
    queryFn: () => {
      const params = new URLSearchParams();
      const trimmed = query.trim();
      if (trimmed) {
        params.set("query", trimmed);
      }
      if (teamCode) {
        params.set("teamCode", teamCode);
      }
      const qs = params.toString();
      return apiFetch<PlayerSearchResponse>(
        `/api/tournament-bonuses/players${qs ? `?${qs}` : ""}`
      );
    },
    enabled,
    staleTime: 60_000,
    placeholderData: (previous) => previous,
  });
}

export function useSaveTournamentBonusPick() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      category,
      pickValue,
    }: {
      category: TournamentBonusCategoryId;
      pickValue: string;
    }) => {
      const turnstileToken = await getTurnstileToken();
      return apiFetch<TournamentBonusPick>("/api/tournament-bonuses/pick", {
        method: "PUT",
        body: JSON.stringify({ category, pickValue, turnstileToken }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tournament-bonuses"] });
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
      queryClient.invalidateQueries({ queryKey: ["leaderboard"] });
    },
  });
}
