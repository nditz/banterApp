let pendingResolver: ((token: string | null) => void) | null = null;

export function getTurnstileToken(): Promise<string | null> {
  const siteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY;
  if (!siteKey) {
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
