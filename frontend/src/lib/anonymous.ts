const ANONYMOUS_ID_KEY = "banter_anonymous_id";
const ANONYMOUS_COOKIE = "banter_anonymous_id";

function generateId(): string {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return `anon-${Date.now()}-${Math.random().toString(36).slice(2, 11)}`;
}

function setCookie(name: string, value: string, days = 365): void {
  if (typeof document === "undefined") return;
  const expires = new Date(Date.now() + days * 86400000).toUTCString();
  document.cookie = `${name}=${encodeURIComponent(value)}; expires=${expires}; path=/; SameSite=Lax`;
}

function getCookie(name: string): string | null {
  if (typeof document === "undefined") return null;
  const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

export interface AnonymousUser {
  id: string;
}

export function getOrCreateAnonymousUser(): AnonymousUser {
  if (typeof window === "undefined") {
    return { id: "server-anonymous" };
  }

  let id = getCookie(ANONYMOUS_COOKIE) ?? localStorage.getItem(ANONYMOUS_ID_KEY);

  if (!id) {
    id = generateId();
    setCookie(ANONYMOUS_COOKIE, id);
    localStorage.setItem(ANONYMOUS_ID_KEY, id);
  }

  return { id };
}

export function getAnonymousId(): string | null {
  if (typeof window === "undefined") return null;
  return getCookie(ANONYMOUS_COOKIE) ?? localStorage.getItem(ANONYMOUS_ID_KEY);
}

export function isMatchLocked(match: {
  kickoffTime: string;
  status?: string;
  isLocked?: boolean;
}): boolean {
  if (match.isLocked) return true;
  const status = match.status?.toUpperCase();
  if (status && ["FT", "LIVE", "HT", "1H", "2H", "AET", "PEN", "FINISHED"].includes(status)) {
    return true;
  }
  return new Date(match.kickoffTime).getTime() <= Date.now();
}
