"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { useSession } from "@/hooks/useSession";
import type { PredictionReceipt } from "@/lib/types";

export function useReceipts() {
  const { data: session, isLoading: sessionLoading } = useSession();
  const termsAccepted = session?.termsAccepted ?? false;

  return useQuery({
    queryKey: ["receipts", session?.userId ?? session?.anonymousUserId ?? "guest"],
    queryFn: () => apiFetch<PredictionReceipt[]>("/api/receipts"),
    enabled: !sessionLoading && termsAccepted,
    retry: 1,
    staleTime: 30_000,
  });
}
