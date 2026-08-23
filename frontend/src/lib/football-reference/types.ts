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

export interface UserPrediction {
  id: string;
  predictionType: string;
  countryId: string | null;
  countryName: string | null;
  countryFlagUrl: string | null;
  playerId: string | null;
  playerName: string | null;
  playerPhotoUrl: string | null;
  competition: string | null;
  season: string | null;
  confidence: number | null;
  isLocked: boolean;
  lockedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface PredictionCategoryInfo {
  predictionType: string;
  label: string;
  description: string;
  requiresCountry: boolean;
  requiresPlayer: boolean;
  pick: UserPrediction | null;
}

export interface UserPredictionsStatus {
  isLocked: boolean;
  lockDeadline: string | null;
  canEdit: boolean;
  competition: string;
  season: string;
  categories: PredictionCategoryInfo[];
}

export interface PredictionAggregateEntry {
  playerId: string | null;
  countryId: string | null;
  name: string;
  country: string | null;
  predictionCount: number;
  percentage: number;
}

export interface PredictionAggregateResponse {
  predictionType: string;
  entries: PredictionAggregateEntry[];
}

export type UserPredictionType =
  | "league_winner"
  | "top_four"
  | "relegated"
  | "best_player"
  | "top_goal_scorer"
  | "top_assist_provider"
  | "golden_boot"
  | "best_young_player"
  | "player_of_the_season"
  | "golden_glove"
  | "surprise_team";

export const PREDICTION_ROUTES: Record<UserPredictionType, string> = {
  league_winner: "/awards",
  top_four: "/awards",
  relegated: "/awards",
  best_player: "/awards",
  top_goal_scorer: "/awards",
  top_assist_provider: "/awards",
  golden_boot: "/awards",
  best_young_player: "/awards",
  player_of_the_season: "/awards",
  golden_glove: "/awards",
  surprise_team: "/awards",
};
