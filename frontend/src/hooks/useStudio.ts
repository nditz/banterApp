"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";
import type { StudioComparison } from "@/lib/types";

const MOCK_FALLBACK: StudioComparison = {
  myTotalPoints: 10,
  myLeagueRank: 2,
  leagueTotal: 3,
  matches: [
    {
      matchId: "m1",
      teamA: "Brazil",
      teamB: "Argentina",
      kickoffTime: new Date(Date.now() + 86400000 * 2).toISOString(),
      picks: [
        { name: "You", role: "me", prediction: "Home Win", predictionType: "result", pointsAwarded: 3 },
        { name: "Boss Wandi", role: "league", prediction: "Away Win", predictionType: "result" },
        { name: "GoalOracle", role: "league", prediction: "Draw", predictionType: "result" },
        { name: "Alex Morgan", role: "pundit", organization: "ESPN", prediction: "Home Win", predictionType: "result" },
        { name: "Rio Ferdinand", role: "pundit", organization: "BBC Sport", prediction: "Draw", predictionType: "result" },
        { name: "Thierry Henry", role: "pundit", organization: "CBS Sports", prediction: "Home Win", predictionType: "result" },
      ],
    },
    {
      matchId: "m2",
      teamA: "France",
      teamB: "Germany",
      kickoffTime: new Date(Date.now() + 86400000 * 5).toISOString(),
      picks: [
        { name: "You", role: "me", prediction: "2-1", predictionType: "correct_score", pointsAwarded: 7 },
        { name: "Boss Wandi", role: "league", prediction: "Home Win", predictionType: "result", pointsAwarded: 3 },
        { name: "Alex Morgan", role: "pundit", organization: "ESPN", prediction: "Home Win", predictionType: "result" },
        { name: "Stephen A. Smith", role: "pundit", organization: "First Take", prediction: "Draw", predictionType: "result" },
      ],
    },
    {
      matchId: "m3",
      teamA: "Spain",
      teamB: "Morocco",
      kickoffTime: new Date(Date.now() + 86400000 * 8).toISOString(),
      picks: [
        { name: "You", role: "me", prediction: "Home Win", predictionType: "result" },
        { name: "Boss Wandi", role: "league", prediction: "Home Win", predictionType: "result" },
        { name: "Alex Morgan", role: "pundit", organization: "ESPN", prediction: "Away Win", predictionType: "result" },
        { name: "Thierry Henry", role: "pundit", organization: "CBS Sports", prediction: "Home Win", predictionType: "result" },
      ],
    },
  ],
};

export function useStudio() {
  return useQuery<StudioComparison>({
    queryKey: ["studio", "comparison"],
    queryFn: async () => {
      try {
        const data = await apiFetch<StudioComparison>("/api/studio/comparison");
        return data.matches?.length ? data : MOCK_FALLBACK;
      } catch (e) {
        if (e instanceof ApiError) return MOCK_FALLBACK;
        throw e;
      }
    },
    staleTime: 30_000,
  });
}
