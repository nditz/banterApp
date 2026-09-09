export type MatchResult = "home" | "draw" | "away";

export type PredictionType = "result" | "correct_score" | "double_chance";

export type DoubleChanceValue = "home_or_draw" | "away_or_draw" | "home_or_away";

export interface Match {
  id: string;
  teamA: string;
  teamB: string;
  teamACode?: string;
  teamBCode?: string;
  homeLogoUrl?: string;
  awayLogoUrl?: string;
  kickoffTime: string;
  group?: string;
  venue?: string;
  stage?: string;
  matchweekNumber?: number;
  status?: string;
  homeScore?: number;
  awayScore?: number;
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

export interface ReceiptPunditTake {
  punditId: string;
  name: string;
  prediction: string;
  sourceUrl?: string | null;
  sourcePlatform?: string | null;
  wasCorrect: boolean;
}

export interface ReceiptStoryCandidate {
  storyType: string;
  rank: number;
  summary: string;
}

export interface PredictionReceipt {
  id: string;
  predictionId: string;
  matchId: string;
  predictionType: PredictionType;
  predictionValue: string;
  pointsAwarded: number;
  auraDelta: number;
  homeScore?: number | null;
  awayScore?: number | null;
  matchStatus: string;
  storyType: string;
  storyTypes: string[];
  punditTakes: ReceiptPunditTake[];
  storyCandidates: ReceiptStoryCandidate[];
  isPublic: boolean;
  settledAt: string;
  createdAt: string;
  match?: Match;
}

export type FeedItemType =
  | "banter"
  | "meme"
  | "news"
  | "leaderboard"
  | "prediction_highlight"
  | "pundit_quote";

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
  author?: string;
  source?: string;
  sourceUrl?: string;
  publishedAt: string;
  likes?: number;
  reactions?: FeedReactions;
  contentLabel?: string;
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
  archetype?: string;
  parodyCue?: string;
  styleSlug?: string;
  isFictionalPersona?: boolean;
  attributionNote?: string;
  sourceUrl?: string;
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
  bonusPointsEnabled?: boolean;
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
  archetype?: string;
  parodyCue?: string;
  styleSlug?: string;
  isFictionalPersona?: boolean;
  attributionNote?: string;
  sourceUrl?: string;
  sourcePlatform?: string;
  avatarSeed?: string;
  wasCorrect?: boolean | null;
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
  followedPunditCount?: number;
  filteringToFollows?: boolean;
}

export interface PunditDirectoryEntry {
  id: string;
  name: string;
  role?: string | null;
  organization?: string | null;
  opinionCount: number;
  predictionCount: number;
  isFollowed: boolean;
  attributionNote?: string | null;
  sourceUrl?: string | null;
  sourcePlatform?: string | null;
  archetype?: string | null;
  parodyCue?: string | null;
  avatarSeed?: string | null;
  isFictionalPersona?: boolean;
}

// ─── Paginated ────────────────────────────────────────────────────────────────

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasMore: boolean;
  feedMode?: "personal" | "pundit";
}
