"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { getTurnstileToken } from "@/lib/turnstile-token";

export interface BracketTeam {
  code: string;
  name: string;
}

export interface BracketSlot {
  slotId: string;
  matchId: string;
  round: string;
  roundOrder: number;
  position: number;
  kind: string;
  teamA: BracketTeam | null;
  teamB: BracketTeam | null;
  ready: boolean;
  pickedWinnerCode: string | null;
  isLocked: boolean;
  kickoffTime: string | null;
  venue: string;
  qualifierLabel: string | null;
  sourceSlotAId: string | null;
  sourceSlotBId: string | null;
}

export interface BracketRound {
  label: string;
  order: number;
  phase: "group" | "knockout" | string;
  slots: BracketSlot[];
}

export interface GroupStanding {
  teamCode: string;
  teamName: string;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
  rank: number;
}

export interface BracketState {
  rounds: BracketRound[];
  picks: Array<{
    slotId: string;
    matchId: string;
    winnerTeamCode: string;
    lockedAt: string | null;
  }>;
  standings: Record<string, GroupStanding[]>;
}

export function useBracket() {
  return useQuery({
    queryKey: ["brackets", "mine"],
    queryFn: () => apiFetch<BracketState>("/api/brackets/mine"),
    retry: 1,
  });
}

export function useSaveBracketPick() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      slotId,
      winnerTeamCode,
    }: {
      slotId: string;
      winnerTeamCode: string;
    }) => {
      const turnstileToken = await getTurnstileToken();
      return apiFetch("/api/brackets/pick", {
        method: "PUT",
        body: JSON.stringify({ slotId, winnerTeamCode, turnstileToken }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["brackets"] });
    },
  });
}
