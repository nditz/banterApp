export type AppNavLink = {
  href: string;
  label: string;
};

/** Desktop primary: Predict / Banter / Studio / Leagues / Season Calls. */
export const DESKTOP_PRIMARY_NAV: readonly AppNavLink[] = [
  { href: "/matchweek", label: "Predict" },
  { href: "/banter", label: "Banter" },
  { href: "/studio", label: "Studio" },
  { href: "/leagues", label: "Leagues" },
  { href: "/awards", label: "Season Calls" },
] as const;

/** Desktop More: pundits, commodity table, receipts, rules. */
export const DESKTOP_OVERFLOW_NAV: readonly AppNavLink[] = [
  { href: "/pundits", label: "Pundits" },
  { href: "/table", label: "Table" },
  { href: "/predictions/history", label: "Receipts" },
  { href: "/rules", label: "Rules" },
] as const;

/**
 * Mobile bottom bar. Studio stays visually central (third of five).
 * Table is demoted to the overflow menu.
 */
export const MOBILE_BOTTOM_NAV: readonly AppNavLink[] = [
  { href: "/matchweek", label: "Predict" },
  { href: "/banter", label: "Banter" },
  { href: "/studio", label: "Studio" },
  { href: "/leagues", label: "Leagues" },
  { href: "/me", label: "Me" },
] as const;

/** Hamburger overflow on small screens. */
export const MOBILE_OVERFLOW_NAV: readonly AppNavLink[] = [
  { href: "/awards", label: "Season Calls" },
  { href: "/pundits", label: "Pundits" },
  { href: "/table", label: "Table" },
  { href: "/predictions/history", label: "Receipts" },
  { href: "/rules", label: "Rules" },
] as const;

export const HOME_QUICK_NAV: readonly AppNavLink[] = [
  { href: "#predictions", label: "Predict" },
  { href: "#banter-feed", label: "Banter" },
  { href: "/studio", label: "Studio" },
  { href: "/leagues", label: "Leagues" },
  { href: "/awards", label: "Season Calls" },
] as const;

export function navPathname(href: string): string {
  const withoutHash = href.split("#")[0];
  return withoutHash && withoutHash.length > 0 ? withoutHash : "/";
}

export function isNavHrefActive(href: string, pathname: string): boolean {
  const path = navPathname(href);
  const hash = href.includes("#") ? href.slice(href.indexOf("#")) : "";

  // Primary Banter dest is /banter. Homepage hash jump is not a Banter-active route.
  if (hash === "#banter-feed" && !href.startsWith("#")) {
    return pathname === "/banter";
  }
  if (href.startsWith("#")) {
    return pathname === "/";
  }
  if (path === "/") {
    return pathname === "/";
  }
  return pathname === path || pathname.startsWith(`${path}/`);
}
