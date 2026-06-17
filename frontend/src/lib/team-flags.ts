/**
 * Maps FIFA / common 3-letter team codes to flagcdn.com country codes.
 * @see https://flagcdn.com
 */
const FIFA_TO_FLAG: Record<string, string> = {
  ALG: "dz",
  ARG: "ar",
  AUS: "au",
  AUT: "at",
  BEL: "be",
  BIH: "ba",
  BOS: "ba",
  BRA: "br",
  CAN: "ca",
  CHI: "cl",
  CIV: "ci",
  CMR: "cm",
  COL: "co",
  CPV: "cv",
  CAP: "cv",
  CRO: "hr",
  CUW: "cw",
  CUR: "cw",
  CZE: "cz",
  COD: "cd",
  ECU: "ec",
  EGY: "eg",
  ENG: "gb-eng",
  ESP: "es",
  FRA: "fr",
  GER: "de",
  GHA: "gh",
  HAI: "ht",
  IRN: "ir",
  IRQ: "iq",
  IRA: "ir",
  ITA: "it",
  IVO: "ci",
  JAM: "jm",
  JOR: "jo",
  JPN: "jp",
  KOR: "kr",
  KSA: "sa",
  MAR: "ma",
  MEX: "mx",
  MOR: "ma",
  NED: "nl",
  NET: "nl",
  NOR: "no",
  NZL: "nz",
  NEW: "nz",
  PAN: "pa",
  PAR: "py",
  PER: "pe",
  POL: "pl",
  POR: "pt",
  QAT: "qa",
  RSA: "za",
  SAU: "sa",
  SCO: "gb-sct",
  SEN: "sn",
  SRB: "rs",
  SUI: "ch",
  SWI: "ch",
  SWE: "se",
  TUN: "tn",
  TUR: "tr",
  URU: "uy",
  USA: "us",
  UZB: "uz",
  TBD: "",
};

/** Resolve ambiguous legacy codes produced from team name prefixes. */
const LEGACY_CODE_BY_NAME: Record<string, string> = {
  Algeria: "dz",
  Iran: "ir",
  Iraq: "iq",
  "South Africa": "za",
  "South Korea": "kr",
  "Bosnia & Herzegovina": "ba",
  "Bosnia and Herzegovina": "ba",
  "Czech Republic": "cz",
  "Curaçao": "cw",
  Curacao: "cw",
  "DR Congo": "cd",
  "Ivory Coast": "ci",
  "Côte d'Ivoire": "ci",
  "New Zealand": "nz",
  "Saudi Arabia": "sa",
  Scotland: "gb-sct",
  England: "gb-eng",
  Netherlands: "nl",
  Switzerland: "ch",
};

export function getFlagCode(teamCode: string, teamName?: string): string | null {
  const normalized = teamCode.trim().toUpperCase();
  if (!normalized || normalized === "TBD") {
    return null;
  }

  if (teamName) {
    const byName = LEGACY_CODE_BY_NAME[teamName.trim()];
    if (byName) {
      return byName;
    }
  }

  if (normalized in FIFA_TO_FLAG) {
    const flag = FIFA_TO_FLAG[normalized];
    return flag || null;
  }

  // Placeholder knockout codes (1A, W73, L101, etc.)
  if (/^(?:[12][A-L]|W\d+|L\d+)/.test(normalized)) {
    return null;
  }

  return null;
}

export function getFlagUrl(teamCode: string, width = 40, teamName?: string): string | null {
  const flagCode = getFlagCode(teamCode, teamName);
  if (!flagCode) {
    return null;
  }

  return `https://flagcdn.com/w${width}/${flagCode}.png`;
}
