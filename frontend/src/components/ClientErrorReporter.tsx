"use client";

import { useEffect } from "react";
import { reportClientError } from "@/lib/api";

export function ClientErrorReporter() {
  useEffect(() => {
    const onError = (event: ErrorEvent) => {
      void reportClientError(event.error ?? event.message, {
        route: window.location.pathname,
        component: "window.onerror",
        metadata: {
          filename: event.filename ?? "",
          lineno: String(event.lineno ?? ""),
        },
      });
    };

    const onUnhandledRejection = (event: PromiseRejectionEvent) => {
      void reportClientError(event.reason, {
        route: window.location.pathname,
        component: "unhandledrejection",
      });
    };

    window.addEventListener("error", onError);
    window.addEventListener("unhandledrejection", onUnhandledRejection);
    return () => {
      window.removeEventListener("error", onError);
      window.removeEventListener("unhandledrejection", onUnhandledRejection);
    };
  }, []);

  return null;
}
