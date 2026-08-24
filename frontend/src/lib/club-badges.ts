/** Premier League club badges via API-Football (same source as live fixture logos). */
const CLUB_BADGE_BY_CODE: Record<string, string> = {
  ARS: "42",
  AVL: "66",
  BOU: "35",
  BRE: "55",
  BHA: "51",
  BRI: "51",
  BUR: "44",
  CHE: "49",
  COV: "71",
  CRY: "52",
  EVE: "45",
  FUL: "36",
  HUL: "64",
  IPS: "57",
  LEE: "63",
  LEI: "46",
  LIV: "40",
  MAC: "50",
  MCI: "50",
  MUN: "33",
  NEW: "34",
  NFO: "65",
  NOT: "65",
  SOU: "41",
  SUN: "746",
  TOT: "47",
  WHU: "48",
  WOL: "39",
};

const CLUB_BADGE_BY_NAME: Record<string, string> = {
  arsenal: "42",
  "aston villa": "66",
  bournemouth: "35",
  "afc bournemouth": "35",
  brentford: "55",
  brighton: "51",
  "brighton & hove albion": "51",
  "brighton and hove albion": "51",
  burnley: "44",
  chelsea: "49",
  coventry: "71",
  "coventry city": "71",
  "crystal palace": "52",
  everton: "45",
  fulham: "36",
  hull: "64",
  "hull city": "64",
  ipswich: "57",
  "ipswich town": "57",
  leeds: "63",
  "leeds united": "63",
  leicester: "46",
  "leicester city": "46",
  liverpool: "40",
  "manchester city": "50",
  "man city": "50",
  "manchester united": "33",
  "man united": "33",
  "man utd": "33",
  newcastle: "34",
  "newcastle united": "34",
  "nottingham forest": "65",
  "nott'm forest": "65",
  "nottm forest": "65",
  forest: "65",
  southampton: "41",
  sunderland: "746",
  tottenham: "47",
  "tottenham hotspur": "47",
  spurs: "47",
  "west ham": "48",
  "west ham united": "48",
  wolves: "39",
  "wolverhampton wanderers": "39",
  "wolverhampton": "39",
};

function badgeUrl(teamId: string): string {
  return `https://media.api-sports.io/football/teams/${teamId}.png`;
}

function normalizeName(name: string): string {
  return name
    .trim()
    .toLowerCase()
    .replace(/['’]/g, "")
    .replace(/\s+/g, " ");
}

/** Club crest URL for a Premier League side. Prefers provider logos, then a known club map. */
export function getClubBadgeUrl(teamCode?: string | null, teamName?: string | null): string | null {
  const code = teamCode?.trim().toUpperCase();
  if (code && CLUB_BADGE_BY_CODE[code]) {
    return badgeUrl(CLUB_BADGE_BY_CODE[code]);
  }

  if (teamName) {
    const byName = CLUB_BADGE_BY_NAME[normalizeName(teamName)];
    if (byName) {
      return badgeUrl(byName);
    }
  }

  return null;
}
