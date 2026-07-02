export interface PunditPersona {
  id: string;
  name: string;
  organization: string;
  archetype: string;
  parodyCue: string;
  styleSlug: string;
  avatarSeed: string;
}

export const PUNDIT_PERSONAS: PunditPersona[] = [
  {
    id: "11111111-1111-1111-1111-111111111101",
    name: "Side-View Gary",
    organization: "Rant & Chips TV",
    archetype: "Touchline rage merchant",
    parodyCue: "Parody · the touchline close-up guy (Neville energy)",
    styleSlug: "touchline-uk",
    avatarSeed: "side-view-gary",
  },
  {
    id: "11111111-1111-1111-1111-111111111102",
    name: "Sofa Captain Rio",
    organization: "Sofa Champions",
    archetype: "Ex-pro captain couch takes",
    parodyCue: "Parody · the velvet sofa legend (Rio energy)",
    styleSlug: "ex-pro-couch",
    avatarSeed: "sofa-captain-rio",
  },
  {
    id: "11111111-1111-1111-1111-111111111103",
    name: "Screamin' Stephen",
    organization: "First Controversy Desk",
    archetype: "Loudest desk in the building",
    parodyCue: "Parody · controversy merchant (Stephen A. energy)",
    styleSlug: "hot-take-desk",
    avatarSeed: "screamin-stephen",
  },
  {
    id: "11111111-1111-1111-1111-111111111104",
    name: "Le Prof Henri",
    organization: "Class on Grass",
    archetype: "Silky studio icon",
    parodyCue: "Parody · the smooth studio legend (Henry energy)",
    styleSlug: "silky-studio",
    avatarSeed: "le-prof-henri",
  },
];

export function findPersonaBySlug(styleSlug: string): PunditPersona | undefined {
  return PUNDIT_PERSONAS.find((p) => p.styleSlug === styleSlug);
}
