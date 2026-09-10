import { hasAdvertisingConsent } from "./advertising-consent";

/**
 * Google AdSense configuration.
 *
 * The publisher (client) id is the same across mobile and desktop — AdSense
 * serves responsive units, so a single loader script + responsive <ins> tags
 * cover every breakpoint.
 *
 * Loading is opt-in: nothing advertising-related is requested until the visitor
 * explicitly grants consent in the first-party prompt. Accepting the terms of use
 * is not advertising consent.
 */

export const ADSENSE_CLIENT =
  process.env.NEXT_PUBLIC_ADSENSE_CLIENT ?? "ca-pub-5886846159925642";

/** Display unit "ball-take-ads" from the AdSense snippet. */
export const ADSENSE_DISPLAY_SLOT =
  slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_DISPLAY") ??
  slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_FEED") ??
  "6603089832";

export const ADSENSE_ENABLED = ADSENSE_CLIENT.length > 0;

/** True only when AdSense is configured and the visitor has explicitly granted consent. */
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

function slotFromEnv(name: string): string | undefined {
  const value = process.env[name]?.trim();
  return value ? value : undefined;
}

/**
 * Placement keys used across the product. Every ad in the app must name one of these so
 * placements stay countable and independently configurable, rather than sharing one unit.
 */
export const AD_PLACEMENT_KEYS = [
  "home-feed-1",
  "home-feed-2",
  "matchweek-between-fixtures",
  "banter-feed",
  "league-standings",
  "table-bottom",
  "rail-left",
  "rail-right",
  "display-top",
] as const;

export type AdPlacementKey = (typeof AD_PLACEMENT_KEYS)[number];

export const AD_SLOT_IDS: Record<string, string> = {
  "rail-left": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_RAIL_LEFT") ?? ADSENSE_DISPLAY_SLOT,
  "rail-right": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_RAIL_RIGHT") ?? ADSENSE_DISPLAY_SLOT,
  "display-top": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_DISPLAY_TOP") ?? ADSENSE_DISPLAY_SLOT,
  "home-feed-1": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_HOME_FEED_1") ?? ADSENSE_DISPLAY_SLOT,
  "home-feed-2": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_HOME_FEED_2") ?? ADSENSE_DISPLAY_SLOT,
  "banter-feed": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_BANTER_FEED") ?? ADSENSE_DISPLAY_SLOT,
  "matchweek-between-fixtures":
    slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_MATCHWEEK") ?? ADSENSE_DISPLAY_SLOT,
  "league-standings": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_SIDEBAR") ?? ADSENSE_DISPLAY_SLOT,
  "table-bottom": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_TABLE_BOTTOM") ?? ADSENSE_DISPLAY_SLOT,
};

export function resolveAdSlotId(slotKey?: string): string {
  if (!slotKey) {
    return ADSENSE_DISPLAY_SLOT;
  }

  return AD_SLOT_IDS[slotKey] ?? ADSENSE_DISPLAY_SLOT;
}

export function hasConfiguredAdSlot(slotKey?: string): boolean {
  return Boolean(resolveAdSlotId(slotKey));
}

export { hasAdvertisingConsent };
