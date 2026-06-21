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
