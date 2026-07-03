"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { useSession } from "@/hooks/useSession";
import { getTurnstileToken } from "@/lib/turnstile-token";
import { mockPredictionHistory } from "@/lib/mock-data";
import type { Prediction } from "@/lib/types";

export interface CreatePredictionPayload {
  matchId: string;
  predictionType: "result" | "correct_score" | "double_chance";
  predictionValue: string;
}

function usePredictionHistoryQueryOptions() {
  const { data: session, isLoading: sessionLoading } = useSession();
  const termsAccepted = session?.termsAccepted ?? false;

  return {
    enabled: !sessionLoading && termsAccepted,
    placeholderData: termsAccepted ? undefined : mockPredictionHistory,
  };
}

export function usePredictions() {
  const queryClient = useQueryClient();
  const historyOptions = usePredictionHistoryQueryOptions();

  const historyQuery = useQuery({
    queryKey: ["predictions", "history"],
    queryFn: () => apiFetch<Prediction[]>("/api/predictions/history"),
    ...historyOptions,
    retry: 1,
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
      queryClient.invalidateQueries({ queryKey: ["leaderboard"] });
    },
  });

  const savePrediction = async (payload: CreatePredictionPayload) => {
    const existing = historyQuery.data?.find(
      (p) =>
        p.matchId === payload.matchId && p.predictionType === payload.predictionType
    );

    if (existing) {
      return updateMutation.mutateAsync({
        ...payload,
        predictionId: existing.id,
      });
    }

    try {
      return await createMutation.mutateAsync(payload);
    } catch (error) {
      const refreshed = await queryClient.fetchQuery({
        queryKey: ["predictions", "history"],
        queryFn: () => apiFetch<Prediction[]>("/api/predictions/history"),
        ...historyOptions,
      });
      const created = refreshed.find(
        (p) =>
          p.matchId === payload.matchId && p.predictionType === payload.predictionType
      );
      if (created) {
        return updateMutation.mutateAsync({
          ...payload,
          predictionId: created.id,
        });
      }
      throw error;
    }
  };

  return {
    history: historyQuery,
    createPrediction: createMutation,
    updatePrediction: updateMutation,
    savePrediction,
    isSaving: createMutation.isPending || updateMutation.isPending,
  };
}

export function usePredictionHistory() {
  const historyOptions = usePredictionHistoryQueryOptions();

  return useQuery({
    queryKey: ["predictions", "history"],
    queryFn: () => apiFetch<Prediction[]>("/api/predictions/history"),
    ...historyOptions,
    retry: 1,
  });
}
