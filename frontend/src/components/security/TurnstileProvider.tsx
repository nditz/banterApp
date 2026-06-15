"use client";

import { useEffect, useRef } from "react";
import { resolveTurnstileToken } from "@/lib/turnstile-token";

export function TurnstileProvider() {
  const containerRef = useRef<HTMLDivElement>(null);
  const widgetIdRef = useRef<string | null>(null);
  const siteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY;

  useEffect(() => {
    if (!siteKey) return;

    const load = () => {
      if (!containerRef.current || !window.turnstile || widgetIdRef.current) return;
      widgetIdRef.current = window.turnstile.render(containerRef.current, {
        sitekey: siteKey,
        theme: "auto",
        execution: "execute",
        callback: (token) => resolveTurnstileToken(token),
        "expired-callback": () => resolveTurnstileToken(null),
      });
    };

    const existing = document.querySelector('script[src*="challenges.cloudflare.com/turnstile"]');
    if (existing) {
      load();
    } else {
      const script = document.createElement("script");
      script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
      script.async = true;
      script.onload = load;
      document.head.appendChild(script);
    }

    const onRequest = () => {
      if (!siteKey) {
        resolveTurnstileToken("dev-bypass");
        return;
      }
      if (widgetIdRef.current && window.turnstile) {
        window.turnstile.execute(widgetIdRef.current);
      }
    };

    window.addEventListener("banter:request-turnstile", onRequest);
    return () => window.removeEventListener("banter:request-turnstile", onRequest);
  }, [siteKey]);

  return <div ref={containerRef} className="sr-only" aria-hidden />;
}
