const ALLOWED_SCHEMES = new Set(["http:", "https:"]);

export function isSafeExternalUrl(url: string | null | undefined): boolean {
  if (!url) return false;
  try {
    const parsed = new URL(url);
    return ALLOWED_SCHEMES.has(parsed.protocol);
  } catch {
    return false;
  }
}

export function safeExternalHref(url: string | null | undefined): string | undefined {
  return isSafeExternalUrl(url) ? url! : undefined;
}
