"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { useSession } from "@/hooks/useSession";
import type { PunditDirectoryEntry } from "@/lib/types";

export function usePunditDirectory() {
  const { data: session } = useSession();
  return useQuery({
    queryKey: [
      "pundits",
      "directory",
      session?.userId ?? session?.anonymousUserId ?? "guest",
    ],
    queryFn: () => apiFetch<PunditDirectoryEntry[]>("/api/pundits?kind=source&pageSize=50"),
    staleTime: 30_000,
  });
}

export function useFollowPundit() {
  const queryClient = useQueryClient();

  const follow = useMutation({
    mutationFn: (punditId: string) =>
      apiFetch<PunditDirectoryEntry>(`/api/pundits/${punditId}/follow`, { method: "POST" }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["pundits"] });
      void queryClient.invalidateQueries({ queryKey: ["studio"] });
      void queryClient.invalidateQueries({ queryKey: ["feed"] });
    },
  });

  const unfollow = useMutation({
    mutationFn: (punditId: string) =>
      apiFetch(`/api/pundits/${punditId}/follow`, { method: "DELETE" }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["pundits"] });
      void queryClient.invalidateQueries({ queryKey: ["studio"] });
      void queryClient.invalidateQueries({ queryKey: ["feed"] });
    },
  });

  return { follow, unfollow };
}
