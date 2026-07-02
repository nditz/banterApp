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

/**
 * Renderable media may be an allowed external http(s) URL or a same-origin, root-relative
 * asset path (e.g. bundled reaction stickers under `/reactions/...`). Rejects protocol-relative
 * (`//host`) and scheme URLs that aren't http(s).
 */
export function isSafeMediaUrl(url: string | null | undefined): boolean {
  if (!url) return false;
  if (url.startsWith("/") && !url.startsWith("//")) {
    return true;
  }
  return isSafeExternalUrl(url);
}
