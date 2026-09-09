"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { useSession } from "@/hooks/useSession";
import type { PunditDirectoryEntry } from "@/lib/types";

function usePunditQueryEnabled() {
  const { isLoading } = useSession();
  return { enabled: !isLoading };
}

export function usePunditDirectory() {
  const options = usePunditQueryEnabled();
  return useQuery({
    queryKey: ["pundits", "directory"],
    queryFn: () => apiFetch<PunditDirectoryEntry[]>("/api/pundits?kind=source&pageSize=50"),
    ...options,
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
