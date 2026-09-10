"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { PRODUCT_METRICS, recordMetric } from "@/lib/metrics";
import type {
  StudioContentType,
  StudioPackGenerateResponse,
  StudioPerspective,
  StudioTone,
} from "@/lib/types";

export interface CreateStudioPackInput {
  contentType: StudioContentType;
  tone: StudioTone;
  perspective?: StudioPerspective | null;
  receiptId?: string | null;
  feedItemId?: string | null;
  projectId?: string | null;
}

export function useGenerateStudioPack() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateStudioPackInput) =>
      apiFetch<StudioPackGenerateResponse>("/api/studio/packs", {
        method: "POST",
        body: JSON.stringify({
          contentType: input.contentType,
          tone: input.tone,
          perspective: input.perspective ?? null,
          receiptId: input.receiptId ?? null,
          feedItemId: input.feedItemId ?? null,
          projectId: input.projectId ?? null,
        }),
      }),
    onSuccess: () => {
      recordMetric(PRODUCT_METRICS.contentGenerated);
      void queryClient.invalidateQueries({ queryKey: ["studio", "stories"] });
      void queryClient.invalidateQueries({ queryKey: ["studio", "packs"] });
    },
  });
}
