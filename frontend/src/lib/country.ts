const STORAGE_KEY = "banter_country_code";

/** ISO 3166-1 alpha-2 code + display name, for the optional country picker. */
export interface CountryOption {
  code: string;
  name: string;
}

/** Common football-playing markets first, then a broad alphabetical list. */
export const COUNTRIES: CountryOption[] = [
  { code: "GB", name: "United Kingdom" },
  { code: "US", name: "United States" },
  { code: "NG", name: "Nigeria" },
  { code: "ZA", name: "South Africa" },
  { code: "KE", name: "Kenya" },
  { code: "GH", name: "Ghana" },
  { code: "IN", name: "India" },
  { code: "AU", name: "Australia" },
  { code: "CA", name: "Canada" },
  { code: "IE", name: "Ireland" },
  { code: "DE", name: "Germany" },
  { code: "FR", name: "France" },
  { code: "ES", name: "Spain" },
  { code: "IT", name: "Italy" },
  { code: "PT", name: "Portugal" },
  { code: "NL", name: "Netherlands" },
  { code: "BE", name: "Belgium" },
  { code: "BR", name: "Brazil" },
  { code: "AR", name: "Argentina" },
  { code: "MX", name: "Mexico" },
  { code: "CO", name: "Colombia" },
  { code: "JP", name: "Japan" },
  { code: "KR", name: "South Korea" },
  { code: "SA", name: "Saudi Arabia" },
  { code: "AE", name: "United Arab Emirates" },
  { code: "EG", name: "Egypt" },
  { code: "MA", name: "Morocco" },
  { code: "SN", name: "Senegal" },
  { code: "CI", name: "Ivory Coast" },
  { code: "CM", name: "Cameroon" },
  { code: "DZ", name: "Algeria" },
  { code: "TN", name: "Tunisia" },
  { code: "CH", name: "Switzerland" },
  { code: "SE", name: "Sweden" },
  { code: "NO", name: "Norway" },
  { code: "DK", name: "Denmark" },
  { code: "PL", name: "Poland" },
  { code: "TR", name: "Turkey" },
  { code: "GR", name: "Greece" },
  { code: "HR", name: "Croatia" },
  { code: "RS", name: "Serbia" },
  { code: "UY", name: "Uruguay" },
  { code: "CL", name: "Chile" },
  { code: "NZ", name: "New Zealand" },
];

/** Persists (or clears) the user's chosen country for the X-Country-Code header. */
export function setCountryCode(code: string | null): void {
  if (typeof window === "undefined") return;
  if (code && code.length === 2) {
    localStorage.setItem(STORAGE_KEY, code.toUpperCase());
  } else {
    localStorage.removeItem(STORAGE_KEY);
  }
}

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
