"use client";

import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { createClient } from "@/lib/supabase/client";

/**
 * Keeps the backend-derived `["session"]` query in sync with the Supabase
 * client auth state. Without this, logging in (or having the token refreshed,
 * or confirming an email in another tab) never invalidates the cached session,
 * so the UI keeps showing the anonymous/logged-out state.
 */
export function useSupabaseAuthSync() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const supabase = createClient();
    if (!supabase) return;

    const {
      data: { subscription },
    } = supabase.auth.onAuthStateChange((event) => {
      if (
        event === "SIGNED_IN" ||
        event === "SIGNED_OUT" ||
        event === "TOKEN_REFRESHED" ||
        event === "USER_UPDATED"
      ) {
        void queryClient.invalidateQueries({ queryKey: ["session"] });
      }
    });

    return () => subscription.unsubscribe();
  }, [queryClient]);
}
