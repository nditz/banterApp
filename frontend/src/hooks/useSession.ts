"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { setCsrfToken } from "@/lib/csrf";
import { getDeviceFingerprint } from "@/lib/fingerprint";
import {
  markTermsAcceptedLocally,
  setStoredRecoveryToken,
  type SessionState,
} from "@/lib/session";

function applySessionState(data: SessionState): SessionState {
  if (data.csrfToken) {
    setCsrfToken(data.csrfToken);
  }
  return data;
}

export function useSession() {
  return useQuery({
    queryKey: ["session"],
    queryFn: async () => {
      const data = await apiFetch<SessionState>("/api/auth/session");
      return applySessionState(data);
    },
    retry: 1,
    staleTime: 30_000,
  });
}

export function useAcceptTerms() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      turnstileToken,
      countryCode = null,
      username = null,
    }: {
      turnstileToken: string | null;
      countryCode?: string | null;
      username?: string | null;
    }) => {
      const deviceFingerprint = await getDeviceFingerprint();
      return apiFetch<SessionState>("/api/auth/session/consent", {
        method: "POST",
        body: JSON.stringify({
          acceptedTerms: true,
          turnstileToken,
          deviceFingerprint,
          countryCode,
          username,
        }),
      });
    },
    onSuccess: (data) => {
      markTermsAcceptedLocally();
      if (data.recoveryToken) {
        setStoredRecoveryToken(data.recoveryToken);
      }
      queryClient.setQueryData(["session"], applySessionState(data));
      queryClient.invalidateQueries({ queryKey: ["matches"] });
      queryClient.invalidateQueries({ queryKey: ["predictions"] });
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });
}

export function useRecoverSession() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      recoveryToken,
      turnstileToken,
      deviceFingerprint,
    }: {
      recoveryToken: string;
      turnstileToken: string | null;
      deviceFingerprint?: string;
    }) =>
      apiFetch<SessionState>("/api/auth/session/recover", {
        method: "POST",
        body: JSON.stringify({ recoveryToken, turnstileToken, deviceFingerprint }),
      }),
    onSuccess: (data) => {
      markTermsAcceptedLocally();
      if (data.recoveryToken) {
        setStoredRecoveryToken(data.recoveryToken);
      }
      queryClient.setQueryData(["session"], applySessionState(data));
      queryClient.invalidateQueries({ queryKey: ["matches"] });
      queryClient.invalidateQueries({ queryKey: ["predictions"] });
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    },
  });
}

export function useSuggestUsername(enabled = true) {
  return useQuery({
    queryKey: ["username-suggest"],
    queryFn: () => apiFetch<{ username: string }>("/api/auth/session/username/suggest"),
    enabled,
    staleTime: 0,
    retry: 1,
  });
}

export function useSetUsername() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      username,
      turnstileToken,
    }: {
      username: string;
      turnstileToken: string | null;
    }) =>
      apiFetch<{ username: string }>("/api/auth/session/username", {
        method: "POST",
        body: JSON.stringify({ username, turnstileToken }),
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(["session"], (prev: SessionState | undefined) =>
        prev ? { ...prev, username: data.username } : prev
      );
      queryClient.invalidateQueries({ queryKey: ["session"] });
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
      queryClient.invalidateQueries({ queryKey: ["leaderboard"] });
    },
  });
}
