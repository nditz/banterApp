/**
 * Maps FIFA / common 3-letter team codes to flagcdn.com country codes.
 * @see https://flagcdn.com
 */
const FIFA_TO_FLAG: Record<string, string> = {
  USA: "us",
  CAN: "ca",
  MEX: "mx",
  JAM: "jm",
  ENG: "gb-eng",
  FRA: "fr",
  GER: "de",
  ESP: "es",
  BRA: "br",
  ARG: "ar",
  URU: "uy",
  COL: "co",
  POR: "pt",
  NED: "nl",
  BEL: "be",
  CRO: "hr",
  ITA: "it",
  SUI: "ch",
  SRB: "rs",
  POL: "pl",
  MAR: "ma",
  SEN: "sn",
  GHA: "gh",
  CMR: "cm",
  JPN: "jp",
  KOR: "kr",
  AUS: "au",
  IRN: "ir",
  ECU: "ec",
  PER: "pe",
  CHI: "cl",
  PAR: "py",
  TBD: "",
};

export function getFlagCode(teamCode: string): string | null {
  const normalized = teamCode.trim().toUpperCase();
  if (!normalized || normalized === "TBD") {
    return null;
  }

  return FIFA_TO_FLAG[normalized] ?? normalized.slice(0, 2).toLowerCase();
}

export function getFlagUrl(teamCode: string, width = 40): string | null {
  const flagCode = getFlagCode(teamCode);
  if (!flagCode) {
    return null;
  }

  return `https://flagcdn.com/w${width}/${flagCode}.png`;
}
