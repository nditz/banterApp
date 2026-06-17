"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
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
    queryFn: () => apiFetch<TournamentBonusStatus>("/api/tournament-bonuses"),
    retry: 1,
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
