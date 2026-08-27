/**
 * Google AdSense configuration.
 *
 * The publisher (client) id is the same across mobile and desktop — AdSense
 * serves responsive units, so a single loader script + responsive <ins> tags
 * cover every breakpoint.
 *
 * To turn the existing placeholder slots into live ad units:
 *   1. Create ad units in the AdSense dashboard for this publisher.
 *   2. Map each internal slot key below to its numeric ad-unit id.
 * Until a key has a numeric id, that slot renders a placeholder (and Auto Ads
 * from the loader script can still fill the page).
 */

export const ADSENSE_CLIENT =
  process.env.NEXT_PUBLIC_ADSENSE_CLIENT ?? "ca-pub-5886846159925642";

export const ADSENSE_ENABLED = ADSENSE_CLIENT.length > 0;

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
