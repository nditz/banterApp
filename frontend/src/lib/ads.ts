import { hasAdvertisingConsent } from "./advertising-consent";

/**
 * Google AdSense configuration.
 *
 * The publisher (client) id is the same across mobile and desktop — AdSense
 * serves responsive units, so a single loader script + responsive <ins> tags
 * cover every breakpoint.
 *
 * Ads must not load before advertising consent (GDPR/ePrivacy). Terms consent
 * is not advertising consent.
 */

export const ADSENSE_CLIENT =
  process.env.NEXT_PUBLIC_ADSENSE_CLIENT ?? "ca-pub-5886846159925642";

export const ADSENSE_ENABLED = ADSENSE_CLIENT.length > 0;

/** True only when AdSense is configured AND the user has granted advertising consent. */
export function canRequestAds(): boolean {
  return ADSENSE_ENABLED && hasAdvertisingConsent();
}

/** Loader script URL used sitewide. */
export const ADSENSE_SCRIPT_SRC = `https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=${ADSENSE_CLIENT}`;

/** CSP allowlists for AdSense + SODAR (used in next.config.ts). */
export const ADSENSE_CSP_CONNECT = [
  "https://*.adtrafficquality.google",
  "https://ep1.adtrafficquality.google",
  "https://ep2.adtrafficquality.google",
  "https://googleads.g.doubleclick.net",
  "https://tpc.googlesyndication.com",
  "https://www.googleadservices.com",
  "https://adservice.google.com",
  "https://fundingchoicesmessages.google.com",
];

export const ADSENSE_CSP_SCRIPT = [
  "https://*.adtrafficquality.google",
  "https://www.googletagservices.com",
  "https://adservice.google.com",
  "https://www.gstatic.com",
];

export const ADSENSE_CSP_FRAME = [
  "https://googleads.g.doubleclick.net",
  "https://tpc.googlesyndication.com",
  "https://*.adtrafficquality.google",
  "https://fundingchoicesmessages.google.com",
  "https://www.google.com",
];

/** Cloudflare Turnstile challenge subdomains (e.g. brunhild.challenges.cloudflare.com). */
export const TURNSTILE_CSP = [
  "https://challenges.cloudflare.com",
  "https://*.challenges.cloudflare.com",
];

/**
 * Maps internal AdSlot `slotId` values to AdSense numeric ad-unit ids.
 * Prefer env vars so units can be wired without a code change.
 */
function slotFromEnv(name: string): string | undefined {
  const value = process.env[name]?.trim();
  return value ? value : undefined;
}

export const AD_SLOT_IDS: Record<string, string> = {
  ...(slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_RAIL_LEFT")
    ? { "rail-left": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_RAIL_LEFT")! }
    : {}),
  ...(slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_RAIL_RIGHT")
    ? { "rail-right": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_RAIL_RIGHT")! }
    : {}),
  ...(slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_SIDEBAR")
    ? { "sidebar-main": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_SIDEBAR")! }
    : {}),
  ...(slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_FEED")
    ? { "feed-0": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_FEED")! }
    : {}),
};

export function resolveAdSlotId(slotKey?: string): string | undefined {
  if (!slotKey) return undefined;
  return AD_SLOT_IDS[slotKey];
}

export function hasConfiguredAdSlot(slotKey?: string): boolean {
  return Boolean(resolveAdSlotId(slotKey));
}
