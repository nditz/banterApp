const STORAGE_KEY = "banter_country_code";

/** ISO 3166-1 alpha-2 from browser locale / language tags. */
export function detectCountryCode(): string {
  if (typeof window === "undefined") {
    return "GB";
  }

  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored && stored.length === 2) {
    return stored.toUpperCase();
  }

  let code = "GB";

  try {
    const locale = navigator.language || "en-GB";
    const region = new Intl.Locale(locale).region;
    if (region && region.length === 2) {
      code = region.toUpperCase();
    }
  } catch {
    const parts = (navigator.language || "").split("-");
    if (parts.length >= 2 && parts[1].length === 2) {
      code = parts[1].toUpperCase();
    }
  }

  localStorage.setItem(STORAGE_KEY, code);
  return code;
}
