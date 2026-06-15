"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { getTurnstileToken } from "@/lib/turnstile-token";
import { mockPredictionHistory } from "@/lib/mock-data";
import type { Prediction } from "@/lib/types";

export interface CreatePredictionPayload {
  matchId: string;
  predictionType: "result" | "correct_score" | "double_chance";
  predictionValue: string;
}

export function usePredictions() {
  const queryClient = useQueryClient();

  const historyQuery = useQuery({
    queryKey: ["predictions", "history"],
    queryFn: async () => {
      try {
        return await apiFetch<Prediction[]>("/api/predictions/history");
      } catch {
        return mockPredictionHistory;
      }
    },
  });

  const createMutation = useMutation({
    mutationFn: async (payload: CreatePredictionPayload) => {
      const turnstileToken = await getTurnstileToken();
      return apiFetch("/api/predictions/create", {
        method: "POST",
        body: JSON.stringify({ ...payload, turnstileToken }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["predictions"] });
      queryClient.invalidateQueries({ queryKey: ["leaderboard"] });
    },
  });

  const updateMutation = useMutation({
    mutationFn: async (payload: CreatePredictionPayload & { id?: string; predictionId?: string }) => {
      const turnstileToken = await getTurnstileToken();
      return apiFetch("/api/predictions/update", {
        method: "PUT",
        body: JSON.stringify({
          predictionId: payload.predictionId ?? payload.id,
          predictionValue: payload.predictionValue,
          turnstileToken,
        }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["predictions"] });
    },
  });

  return {
    history: historyQuery,
    createPrediction: createMutation,
    updatePrediction: updateMutation,
  };
}

export function usePredictionHistory() {
  return useQuery({
    queryKey: ["predictions", "history"],
    queryFn: async () => {
      try {
        return await apiFetch<Prediction[]>("/api/predictions/history");
      } catch {
        return mockPredictionHistory;
      }
    },
  });
}
