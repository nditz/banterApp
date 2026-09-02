"use client";

import { useEffect, useState } from "react";
import { usePathname } from "next/navigation";
import { useSupabaseUser } from "@/hooks/useSupabaseUser";

export function SignedInNotice() {
  const pathname = usePathname();
  const { isSignedIn, displayName, email } = useSupabaseUser();
  const [pending, setPending] = useState(false);
  const [hidden, setHidden] = useState(false);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    let greet = false;
    try {
      greet = sessionStorage.getItem("banter_just_signed_in") === "1";
      if (greet) sessionStorage.removeItem("banter_just_signed_in");
    } catch {
      greet = false;
    }

    if (params.get("signedIn") === "1") {
      greet = true;
      params.delete("signedIn");
      const query = params.toString();
      const next = `${pathname}${query ? `?${query}` : ""}${window.location.hash}`;
      window.history.replaceState(null, "", next);
    }

    if (!greet) return;

    const id = window.setTimeout(() => setPending(true), 0);
    return () => window.clearTimeout(id);
  }, [pathname]);

  useEffect(() => {
    if (!pending || !isSignedIn) return;
    const timer = window.setTimeout(() => setHidden(true), 4200);
    return () => window.clearTimeout(timer);
  }, [isSignedIn, pending]);

  if (hidden || !pending || !isSignedIn) return null;

  const who = displayName || email;
  const message = who ? `You're signed in as ${who}` : "You're signed in";

  return (
    <div
      role="status"
      className="pointer-events-none fixed inset-x-0 top-[4.25rem] z-[60] flex justify-center px-4"
    >
      <p className="rounded-full border border-border bg-card/95 px-3.5 py-1.5 text-xs font-medium text-foreground shadow-md backdrop-blur-sm">
        {message}
      </p>
    </div>
  );
}
