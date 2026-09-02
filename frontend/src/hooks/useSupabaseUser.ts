"use client";

import { useEffect, useState } from "react";
import { getSupabaseAvatarUrl, getSupabaseDisplayName } from "@/lib/avatars";
import { createClient } from "@/lib/supabase/client";

export interface SupabaseUserInfo {
  email: string | null;
  displayName: string | null;
  avatarUrl: string | undefined;
  userId: string | null;
  isSignedIn: boolean;
}

const signedOut: SupabaseUserInfo = {
  email: null,
  displayName: null,
  avatarUrl: undefined,
  userId: null,
  isSignedIn: false,
};

/**
 * Current Supabase user for header/account UI. Stays in sync with sign-in,
 * sign-out, and token refresh via onAuthStateChange.
 */
export function useSupabaseUser(): SupabaseUserInfo {
  const [user, setUser] = useState<SupabaseUserInfo>(signedOut);

  useEffect(() => {
    const supabase = createClient();
    if (!supabase) return;

    let active = true;

    const apply = (next: {
      id: string;
      email?: string | null;
      user_metadata?: Record<string, unknown> | null;
      identities?: Array<{ identity_data?: Record<string, unknown> | null }> | null;
    } | null) => {
      if (!active) return;
      if (!next) {
        setUser(signedOut);
        return;
      }
      setUser({
        email: next.email ?? null,
        displayName: getSupabaseDisplayName(next),
        avatarUrl: getSupabaseAvatarUrl(next),
        userId: next.id,
        isSignedIn: true,
      });
    };

    void supabase.auth.getUser().then(({ data }) => apply(data.user));

    const {
      data: { subscription },
    } = supabase.auth.onAuthStateChange((_event, session) => {
      apply(session?.user ?? null);
    });

    return () => {
      active = false;
      subscription.unsubscribe();
    };
  }, []);

  return user;
}
