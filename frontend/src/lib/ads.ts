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

/**
 * Maps internal AdSlot `slotId` values to AdSense numeric ad-unit ids.
 * Fill these in once the units exist in the AdSense dashboard.
 */
export const AD_SLOT_IDS: Record<string, string> = {
  // "rail-left": "1234567890",
  // "rail-right": "1234567890",
  // "sidebar-main": "1234567890",
  // "feed-0": "1234567890",
};

export function resolveAdSlotId(slotKey?: string): string | undefined {
  if (!slotKey) return undefined;
  return AD_SLOT_IDS[slotKey];
}
