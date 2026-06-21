const RECOVERY_TOKEN_KEY = "banter_recovery_token";
const TERMS_ACCEPTED_KEY = "banter_terms_accepted";

export function getStoredRecoveryToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(RECOVERY_TOKEN_KEY);
}

export function setStoredRecoveryToken(token: string): void {
  if (typeof window === "undefined") return;
  localStorage.setItem(RECOVERY_TOKEN_KEY, token);
}

export function clearStoredRecoveryToken(): void {
  if (typeof window === "undefined") return;
  localStorage.removeItem(RECOVERY_TOKEN_KEY);
}

export function markTermsAcceptedLocally(): void {
  if (typeof window === "undefined") return;
  localStorage.setItem(TERMS_ACCEPTED_KEY, "1");
}

export function hasLocalTermsFlag(): boolean {
  if (typeof window === "undefined") return false;
  return localStorage.getItem(TERMS_ACCEPTED_KEY) === "1";
}

export interface SessionState {
  authenticated: boolean;
  anonymous: boolean;
  termsAccepted: boolean;
  recoveryToken: string | null;
  userId: string | null;
  anonymousUserId: string | null;
  csrfToken?: string | null;
  isPlatformAdmin?: boolean;
}
