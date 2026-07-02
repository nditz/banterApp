"use client";

import { useEffect, useRef, useState } from "react";

interface TurnstileWidgetProps {
  onToken: (token: string | null) => void;
  theme?: "light" | "dark" | "auto";
}

export function TurnstileWidget({ onToken, theme = "auto" }: TurnstileWidgetProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const widgetIdRef = useRef<string | null>(null);
  const onTokenRef = useRef(onToken);
  const siteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY;
  const [errorCode, setErrorCode] = useState<string | null>(null);

  useEffect(() => {
    onTokenRef.current = onToken;
  }, [onToken]);

  useEffect(() => {
    if (!siteKey) {
      onTokenRef.current("dev-bypass");
      return;
    }

    const renderWidget = () => {
      if (!containerRef.current || !window.turnstile) {
        return;
      }

      if (widgetIdRef.current) {
        window.turnstile.remove(widgetIdRef.current);
      }

      widgetIdRef.current = window.turnstile.render(containerRef.current, {
        sitekey: siteKey,
        theme,
        callback: (token) => {
          setErrorCode(null);
          onTokenRef.current(token);
        },
        "expired-callback": () => onTokenRef.current(null),
        "error-callback": (code) => {
          setErrorCode(code ?? "unknown");
          onTokenRef.current(null);
        },
      });
    };

    const existing = document.querySelector('script[src*="challenges.cloudflare.com/turnstile"]');
    if (existing && window.turnstile) {
      renderWidget();
    } else if (existing) {
      existing.addEventListener("load", renderWidget, { once: true });
    } else {
      const script = document.createElement("script");
      script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
      script.async = true;
      script.onload = renderWidget;
      document.head.appendChild(script);
    }

    return () => {
      if (widgetIdRef.current && window.turnstile) {
        window.turnstile.remove(widgetIdRef.current);
        widgetIdRef.current = null;
      }
    };
  }, [siteKey, theme]);

  if (!siteKey) {
    return (
      <p className="text-[11px] text-muted-foreground">
        Human verification runs in production (Turnstile not configured locally).
      </p>
    );
  }

  return (
    <div>
      <div ref={containerRef} className="min-h-[65px]" />
      {errorCode && (
        <p className="mt-1 text-[11px] text-destructive" role="alert">
          Verification could not load (error {errorCode}). Refresh and try again — if it
          persists, this domain may not be authorized for the site key.
        </p>
      )}
    </div>
  );
}
