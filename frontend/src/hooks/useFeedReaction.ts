"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { FeedReactions } from "@/lib/types";

export type ReactionKind = "agree" | "stale" | "disagree";

export function useFeedReaction(itemId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (reaction: ReactionKind) =>
      apiFetch<FeedReactions>(`/api/feed/${itemId}/react`, {
        method: "POST",
        body: JSON.stringify({ reaction }),
      }),
    onSuccess: (updatedReactions) => {
      // Optimistically patch the cached feed pages so counts update immediately
      queryClient.setQueriesData<unknown>(
        { queryKey: ["feed"], exact: false },
        (old: unknown) => {
          if (!old || typeof old !== "object") return old;
          const patchItems = (items: unknown[]) =>
            items.map((item) => {
              if (
                item &&
                typeof item === "object" &&
                (item as { id?: string }).id === itemId
              ) {
                return { ...(item as object), reactions: updatedReactions };
              }
              return item;
            });

          const data = old as Record<string, unknown>;
          if (Array.isArray(data.items)) {
            return { ...data, items: patchItems(data.items) };
          }
          if (Array.isArray(old)) {
            return patchItems(old);
          }
          return old;
        }
      );
    },
  });
}
