"use client";

import { useEffect, useState } from "react";
import { createClient } from "@/lib/supabase/client";

interface SupabaseUserInfo {
  email: string | null;
}

/**
 * Exposes the current Supabase user's email for display in the UI. Stays in
 * sync with sign-in / sign-out / token refresh via onAuthStateChange.
 */
export function useSupabaseUser(): SupabaseUserInfo {
  const [email, setEmail] = useState<string | null>(null);

  useEffect(() => {
    const supabase = createClient();
    if (!supabase) return;

    let active = true;

    void supabase.auth.getUser().then(({ data }) => {
      if (active) setEmail(data.user?.email ?? null);
    });

    const {
      data: { subscription },
    } = supabase.auth.onAuthStateChange((_event, session) => {
      setEmail(session?.user?.email ?? null);
    });

    return () => {
      active = false;
      subscription.unsubscribe();
    };
  }, []);

  return { email };
}
