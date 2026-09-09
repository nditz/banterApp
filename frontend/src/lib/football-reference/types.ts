export interface FootballCountry {
  id: string;
  name: string;
  code: string | null;
  flagUrl: string | null;
  continent: string | null;
  fifaRanking: number | null;
  isActive: boolean;
}

export interface PlayerStatsSummary {
  goals: number;
  assists: number;
  matchesPlayed: number;
  rating: number | null;
}

export interface FootballPlayer {
  id: string;
  displayName: string;
  knownName: string | null;
  position: string | null;
  photoUrl: string | null;
  clubName: string | null;
  countryId: string | null;
  countryName: string | null;
  countryCode: string | null;
  countryFlagUrl: string | null;
  stats: PlayerStatsSummary | null;
}

export interface LeaderboardEntry {
  rank: number | null;
  value: number;
  playerName: string;
  photoUrl: string | null;
  countryName: string | null;
  countryCode: string | null;
  countryFlagUrl: string | null;
  sourceProvider: string | null;
  sourceUpdatedAt: string | null;
}
