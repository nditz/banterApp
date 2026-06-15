const CSRF_COOKIE = "banter_csrf";
const CSRF_STORAGE = "banter_csrf";

export function setCsrfToken(token: string): void {
  if (typeof window === "undefined") return;
  sessionStorage.setItem(CSRF_STORAGE, token);
}

export function getCsrfToken(): string | null {
  if (typeof window === "undefined") return null;
  const stored = sessionStorage.getItem(CSRF_STORAGE);
  if (stored) return stored;

  const match = document.cookie.match(new RegExp(`(?:^|; )${CSRF_COOKIE}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}
