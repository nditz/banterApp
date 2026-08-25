export interface AdminOverview {
  totalRssItems: number;
  totalYoutubeItems: number;
  totalOpinions: number;
  totalPredictions: number;
  itemsNeedingReview: number;
  failedJobsLast24h: number;
  openAiRequestsLast24h: number;
  youtubeQuotaAvailable: boolean;
  latestSuccessfulSyncAt: string | null;
  latestFailedSyncAt: string | null;
  jobsEnabled: boolean;
  openAiConfigured: boolean;
}

export interface AdminJob {
  jobKey: string;
  displayName: string;
  description: string;
  status: "idle" | "running" | "paused" | "failed" | "disabled";
  schedule: string | null;
  lastRunAt: string | null;
  nextRunAt: string | null;
  lastSuccessAt: string | null;
  lastFailureAt: string | null;
  averageDurationMs: number | null;
  failureCount: number;
  successCount: number;
  enabled: boolean;
  paused: boolean;
  canRunManually: boolean;
  canPause: boolean;
  isStub: boolean;
}

export interface AdminJobRun {
  runId: string;
  jobKey: string;
  status: string;
  startedAt: string;
  finishedAt: string | null;
  durationMs: number | null;
  itemsProcessed: number;
  itemsCreated: number;
  itemsUpdated: number;
  itemsSkipped: number;
  itemsFailed: number;
  errorMessage: string | null;
  metadataJson: string | null;
}

export interface OperationalErrorItem {
  id: string;
  source: string;
  jobKey: string | null;
  severity: string;
  message: string;
  errorCode: string;
  status: string;
  firstSeenAt: string;
  lastSeenAt: string;
  count: number;
  resolvedAt: string | null;
  requestId: string | null;
  jobRunId: string | null;
  sourceItemId: string | null;
  provider: string | null;
}

export interface OperationalErrorDetail extends OperationalErrorItem {
  fingerprint: string;
  environment: string;
  errorType: string | null;
  messageInternal: string | null;
  stackTrace: string | null;
  route: string | null;
  method: string | null;
  statusCode: number | null;
  userId: string | null;
  adminUserId: string | null;
  providerRequestId: string | null;
  metadataJson: string | null;
  createdAt: string;
  updatedAt: string;
  detailAvailable: boolean;
}

/** @deprecated Use OperationalErrorItem */
export type IngestionErrorItem = OperationalErrorItem;

export interface AdminSource {
  sourceId: string;
  type: string;
  name: string;
  url: string | null;
  enabled: boolean;
  lastSyncAt: string | null;
  lastSuccessAt: string | null;
  lastErrorAt: string | null;
  itemsIngested: number;
  failureCount: number;
}

export interface AdminSourceItem {
  id: string;
  title: string;
  sourceName: string;
  sourceType: string;
  publishedAt: string | null;
  fetchedAt: string;
  processedAt: string | null;
  status: string;
  hasRawText: boolean;
  hasPredictions: boolean;
  needsHumanReview: boolean;
  processingError: string | null;
}

export interface AdminReviewItem {
  id: string;
  punditName: string;
  opinion: string;
  prediction: string | null;
  predictionType: string | null;
  confidence: number | null;
  evidenceQuote: string | null;
  isDirectQuote: boolean;
  needsHumanReview: boolean;
  reviewStatus: string;
  sourceTitle: string;
  sourceName: string;
  sourceType: string;
  createdAt: string;
}

export interface LaunchChecklistItem {
  label: string;
  passed: boolean;
}

export interface FootballDataOverview {
  countriesCount: number;
  playersCount: number;
  statsCount: number;
  leaderboardEntriesCount: number;
  lastSyncAt: string | null;
  failedSyncCount: number;
  currentProvider: string;
  competition: string;
  season: string;
  recentJobs: Array<{
    jobName: string;
    status: string;
    startedAt: string;
    finishedAt: string | null;
  }>;
}

export interface FootballCountryAdminItem {
  id: string;
  name: string;
  code: string | null;
  flagUrl: string | null;
  isActive: boolean;
  externalProvider: string | null;
  externalId: string | null;
  metadataPreview: string | null;
  updatedAt: string;
}

export interface FootballPlayerAdminItem {
  id: string;
  displayName: string;
  position: string | null;
  photoUrl: string | null;
  countryId: string | null;
  countryName: string | null;
  isActive: boolean;
  externalProvider: string | null;
  externalId: string | null;
  metadataPreview: string | null;
  updatedAt: string;
}

export interface FootballLeaderboardsAdminResponse {
  leaderboardType: string;
  competition: string;
  season: string;
  entries: Array<{
    id: string;
    rank: number | null;
    value: number;
    playerName: string;
    countryName: string | null;
    sourceProvider: string | null;
    sourceUpdatedAt: string | null;
  }>;
}

export type AdminIdentitySource = "supabase" | "database";

export const ACCOUNT_STATUSES = [
  "Active",
  "PendingVerification",
  "Suspended",
  "Banned",
] as const;

export type AccountStatusValue = (typeof ACCOUNT_STATUSES)[number];

export interface AdminUserListItem {
  id: string;
  email: string;
  displayName: string;
  countryCode: string | null;
  accountStatus: string;
  isPlatformAdmin: boolean;
  emailConfirmedAt: string | null;
  termsAcceptedAt: string | null;
  createdAt: string;
  predictionCount: number;
  leagueCount: number;
}

export interface AdminUserListResponse {
  items: AdminUserListItem[];
  page: number;
  pageSize: number;
  total: number;
  identitySource: AdminIdentitySource;
  warning: string | null;
}

export interface AdminUserIdentity {
  createdAt: string | null;
  lastSignInAt: string | null;
  emailConfirmedAt: string | null;
  providers: string[];
  isBanned: boolean;
}

export interface AdminUserActivity {
  predictionCount: number;
  leagueCount: number;
  generatedContentCount: number;
  lastPredictionAt: string | null;
}

export interface AdminUserLeagueMembership {
  leagueId: string;
  name: string;
  kind: string;
  isLeagueAdmin: boolean;
}

export interface AdminUserDetail {
  id: string;
  email: string;
  displayName: string;
  countryCode: string | null;
  avatar: string | null;
  accountStatus: string;
  isPlatformAdmin: boolean;
  isAllowlisted: boolean;
  emailConfirmedAt: string | null;
  termsAcceptedAt: string | null;
  createdAt: string;
  identity: AdminUserIdentity | null;
  activity: AdminUserActivity;
  leagues: AdminUserLeagueMembership[];
  identitySource: AdminIdentitySource;
}

export interface AdminAuditLogItem {
  id: string;
  adminUserId: string;
  action: string;
  targetType: string;
  targetId: string | null;
  metadataJson: string | null;
  ipAddress: string | null;
  createdAt: string;
}

export interface AdminAuditLogResponse {
  items: AdminAuditLogItem[];
  page: number;
  pageSize: number;
  total: number;
  availableActions: string[];
}
