let pendingResolver: ((token: string | null) => void) | null = null;

export function getTurnstileToken(): Promise<string | null> {
  const siteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY;
  if (!siteKey) {
    if (process.env.NODE_ENV === "production") {
      return Promise.reject(
        new Error("NEXT_PUBLIC_TURNSTILE_SITE_KEY is required in production.")
      );
    }
    return Promise.resolve("dev-bypass");
  }

  return new Promise((resolve) => {
    pendingResolver = resolve;
    window.dispatchEvent(new CustomEvent("banter:request-turnstile"));
  });
}

export function resolveTurnstileToken(token: string | null): void {
  pendingResolver?.(token);
  pendingResolver = null;
}
