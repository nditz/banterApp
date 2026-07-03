"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { getTurnstileToken } from "@/lib/turnstile-token";
import type {
  PredictionAggregateResponse,
  UserPrediction,
  UserPredictionsStatus,
} from "@/lib/football-reference/types";

const key = ["user-predictions"] as const;

export function useUserPredictions() {
  return useQuery({
    queryKey: [...key, "status"],
    queryFn: () => apiFetch<UserPredictionsStatus>("/api/user/predictions"),
  });
}

export function usePredictionAggregates(type?: string) {
  const params = new URLSearchParams();
  if (type) params.set("type", type);
  const qs = params.toString();

  return useQuery({
    queryKey: [...key, "aggregates", type ?? "all"],
    queryFn: () =>
      apiFetch<PredictionAggregateResponse>(
        `/api/predictions/aggregates${qs ? `?${qs}` : ""}`
      ),
  });
}

export function useCreateUserPrediction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: {
      predictionType: string;
      countryId?: string;
      playerId?: string;
      confidence?: number;
    }) => {
      const turnstileToken = await getTurnstileToken();
      return apiFetch<UserPrediction>("/api/user/predictions", {
        method: "POST",
        body: JSON.stringify({ ...payload, turnstileToken }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...key] });
    },
  });
}

export function useUpdateUserPrediction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      ...payload
    }: {
      id: string;
      countryId?: string;
      playerId?: string;
      confidence?: number;
    }) => {
      const turnstileToken = await getTurnstileToken();
      return apiFetch<UserPrediction>(`/api/user/predictions/${id}`, {
        method: "PUT",
        body: JSON.stringify({ ...payload, turnstileToken }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...key] });
    },
  });
}
