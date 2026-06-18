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
        {
          name: "Side-View Gary",
          role: "pundit",
          organization: "Rant & Chips TV",
          archetype: "Touchline rage merchant",
          parodyCue: "Parody · the touchline close-up guy (Neville energy)",
          styleSlug: "touchline-uk",
          isFictionalPersona: true,
          prediction: "Home Win",
          predictionType: "result",
        },
        {
          name: "Sofa Captain Rio",
          role: "pundit",
          organization: "Sofa Champions",
          archetype: "Ex-pro captain couch takes",
          parodyCue: "Parody · the velvet sofa legend (Rio energy)",
          styleSlug: "ex-pro-couch",
          isFictionalPersona: true,
          prediction: "Draw",
          predictionType: "result",
        },
        {
          name: "Le Prof Henri",
          role: "pundit",
          organization: "Class on Grass",
          archetype: "Silky studio icon",
          parodyCue: "Parody · the smooth studio legend (Henry energy)",
          styleSlug: "silky-studio",
          isFictionalPersona: true,
          prediction: "Home Win",
          predictionType: "result",
        },
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
        {
          name: "Side-View Gary",
          role: "pundit",
          organization: "Rant & Chips TV",
          parodyCue: "Parody · the touchline close-up guy (Neville energy)",
          isFictionalPersona: true,
          prediction: "Home Win",
          predictionType: "result",
        },
        {
          name: "Screamin' Stephen",
          role: "pundit",
          organization: "First Controversy Desk",
          parodyCue: "Parody · controversy merchant (Stephen A. energy)",
          isFictionalPersona: true,
          prediction: "Draw",
          predictionType: "result",
        },
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
        {
          name: "Side-View Gary",
          role: "pundit",
          organization: "Rant & Chips TV",
          parodyCue: "Parody · the touchline close-up guy (Neville energy)",
          isFictionalPersona: true,
          prediction: "Away Win",
          predictionType: "result",
        },
        {
          name: "Sofa Captain Rio",
          role: "pundit",
          organization: "Sofa Champions",
          parodyCue: "Parody · the velvet sofa legend (Rio energy)",
          isFictionalPersona: true,
          prediction: "Home Win",
          predictionType: "result",
        },
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
