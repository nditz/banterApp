export type MatchResult = "home" | "draw" | "away";

export type PredictionType = "result" | "correct_score" | "double_chance";

export type DoubleChanceValue = "home_or_draw" | "away_or_draw" | "home_or_away";

export interface Match {
  id: string;
  teamA: string;
  teamB: string;
  teamACode?: string;
  teamBCode?: string;
  kickoffTime: string;
  group?: string;
  venue?: string;
  stage?: string;
  status?: string;
  isLocked?: boolean;
}

export interface Prediction {
  id: string;
  matchId: string;
  predictionType: PredictionType;
  predictionValue: string;
  pointsAwarded?: number;
  createdAt: string;
  match?: Match;
}

export type FeedItemType =
  | "banter"
  | "meme"
  | "news"
  | "leaderboard"
  | "prediction_highlight";

export type FeedMediaType = "image" | "gif" | "video" | "clip";

export interface FeedMedia {
  type: FeedMediaType;
  url: string;
  posterUrl?: string;
  audioUrl?: string;
  alt?: string;
}

export interface FeedReactions {
  agree: number;
  stale: number;
  disagree: number;
}

export interface FeedItem {
  id: string;
  type: FeedItemType;
  title: string;
  body: string;
  imageUrl?: string;
  media?: FeedMedia;
  source?: string;
  sourceUrl?: string;
  publishedAt: string;
  likes?: number;
  reactions?: FeedReactions;
}

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  displayName: string;
  avatarUrl?: string;
  points: number;
  correctPredictions?: number;
  totalPredictions?: number;
  isPundit?: boolean;
  organization?: string;
  isCurrentUser?: boolean;
}

export interface LeaderboardView {
  entries: LeaderboardEntry[];
  me: LeaderboardEntry | null;
  totalPlayers: number;
}

export type LeagueKind = "custom" | "global" | "country";

export interface League {
  id: string;
  name: string;
  inviteCode: string;
  memberCount: number;
  maxMembers?: number;
  isAdmin?: boolean;
  myDisplayName?: string;
  ownerName?: string;
  rank?: number;
  points?: number;
  kind?: LeagueKind;
  countryCode?: string;
}

export interface LeagueLimits {
  customLeaguesUsed: number;
  customLeaguesMax: number;
  totalLeaguesUsed: number;
  totalLeaguesMax: number;
}

export interface MyLeaguesPayload {
  leagues: League[];
  limits: LeagueLimits;
}

export interface LeaguePreview {
  id: string;
  name: string;
  inviteCode: string;
  memberCount: number;
  maxMembers: number;
  isFull: boolean;
}

// ─── Content Studio ──────────────────────────────────────────────────────────

export type StudioPickRole = "me" | "league" | "pundit";

export interface StudioPickEntry {
  name: string;
  role: StudioPickRole;
  organization?: string;
  prediction: string;
  predictionType: string;
  pointsAwarded?: number;
}

export interface StudioMatchComparison {
  matchId: string;
  teamA: string;
  teamB: string;
  kickoffTime: string;
  status?: string;
  actualResult?: string;
  picks: StudioPickEntry[];
}

export interface StudioComparison {
  matches: StudioMatchComparison[];
  myTotalPoints: number;
  myLeagueRank?: number;
  leagueTotal?: number;
}

// ─── Paginated ────────────────────────────────────────────────────────────────

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasMore: boolean;
}
