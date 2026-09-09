import { hasAdvertisingConsent, readAdvertisingConsent } from "./advertising-consent";

/**
 * Google AdSense configuration.
 *
 * The publisher (client) id is the same across mobile and desktop — AdSense
 * serves responsive units, so a single loader script + responsive <ins> tags
 * cover every breakpoint.
 *
 * Ads load when the publisher id is set unless the visitor has denied
 * advertising. Google Funding Choices / AdSense handles EEA consent in the
 * AdSense dashboard. An explicit local deny still wins.
 */

export const ADSENSE_CLIENT =
  process.env.NEXT_PUBLIC_ADSENSE_CLIENT ?? "ca-pub-5886846159925642";

/** Display unit "ball-take-ads" from the AdSense snippet. */
export const ADSENSE_DISPLAY_SLOT =
  slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_DISPLAY") ??
  slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_FEED") ??
  "6603089832";

export const ADSENSE_ENABLED = ADSENSE_CLIENT.length > 0;

/** True when AdSense is configured and the visitor has not denied ads. */
export function canRequestAds(): boolean {
  return ADSENSE_ENABLED && readAdvertisingConsent() !== "denied";
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

export const AD_SLOT_IDS: Record<string, string> = {
  "rail-left": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_RAIL_LEFT") ?? ADSENSE_DISPLAY_SLOT,
  "rail-right": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_RAIL_RIGHT") ?? ADSENSE_DISPLAY_SLOT,
  "sidebar-main": slotFromEnv("NEXT_PUBLIC_ADSENSE_SLOT_SIDEBAR") ?? ADSENSE_DISPLAY_SLOT,
  feed: ADSENSE_DISPLAY_SLOT,
  display: ADSENSE_DISPLAY_SLOT,
};

export function resolveAdSlotId(slotKey?: string): string {
  if (!slotKey) {
    return ADSENSE_DISPLAY_SLOT;
  }

  if (AD_SLOT_IDS[slotKey]) {
    return AD_SLOT_IDS[slotKey];
  }

  if (slotKey.startsWith("feed-")) {
    return AD_SLOT_IDS.feed;
  }

  if (slotKey.startsWith("rail-")) {
    return AD_SLOT_IDS["rail-left"];
  }

  return ADSENSE_DISPLAY_SLOT;
}

export function hasConfiguredAdSlot(slotKey?: string): boolean {
  return Boolean(resolveAdSlotId(slotKey));
}

export { hasAdvertisingConsent };
