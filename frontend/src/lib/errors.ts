export const ErrorCodes = {
  ValidationError: "VALIDATION_ERROR",
  AuthenticationRequired: "AUTHENTICATION_REQUIRED",
  Forbidden: "FORBIDDEN",
  NotFound: "NOT_FOUND",
  Conflict: "CONFLICT",
  RateLimited: "RATE_LIMITED",
  CsrfFailed: "CSRF_FAILED",
  BadRequest: "BAD_REQUEST",
  InternalServerError: "INTERNAL_SERVER_ERROR",
  DatabaseError: "DATABASE_ERROR",
  ExternalApiError: "EXTERNAL_API_ERROR",
  OpenAiApiError: "OPENAI_API_ERROR",
  YouTubeApiError: "YOUTUBE_API_ERROR",
  RssFetchError: "RSS_FETCH_ERROR",
  JobFailed: "JOB_FAILED",
  AiOutputValidationError: "AI_OUTPUT_VALIDATION_ERROR",
  ConfigurationError: "CONFIGURATION_ERROR",
  UnknownError: "UNKNOWN_ERROR",
} as const;

export type ErrorCode = (typeof ErrorCodes)[keyof typeof ErrorCodes];

export interface ApiErrorBody {
  code: string;
  message: string;
  requestId?: string;
  details?: Record<string, string[] | string>;
  detail?: string;
}

export interface ApiErrorEnvelope {
  success: false;
  error: ApiErrorBody;
}

export function isApiErrorEnvelope(value: unknown): value is ApiErrorEnvelope {
  if (!value || typeof value !== "object") return false;
  const obj = value as Record<string, unknown>;
  return obj.success === false && typeof obj.error === "object" && obj.error !== null;
}

const SAFE_MESSAGES: Partial<Record<string, string>> = {
  [ErrorCodes.ValidationError]: "Please check the submitted fields.",
  [ErrorCodes.AuthenticationRequired]: "Please sign in to continue.",
  [ErrorCodes.Forbidden]: "You do not have permission to perform this action.",
  [ErrorCodes.NotFound]: "The requested resource was not found.",
  [ErrorCodes.Conflict]: "This action could not be completed.",
  [ErrorCodes.RateLimited]: "Too many requests. Please wait and try again.",
  [ErrorCodes.CsrfFailed]: "Your session expired. Please refresh and try again.",
  [ErrorCodes.InternalServerError]: "Something went wrong. Please try again.",
  [ErrorCodes.OpenAiApiError]: "AI service is temporarily unavailable.",
  [ErrorCodes.YouTubeApiError]: "We could not load this video right now.",
  [ErrorCodes.RssFetchError]: "We could not load this feed right now.",
  [ErrorCodes.JobFailed]: "A background task failed. Our team has been notified.",
};

export function getSafeMessageForCode(code: string, fallback?: string): string {
  return SAFE_MESSAGES[code] ?? fallback ?? "Something went wrong. Please refresh and try again.";
}

export function formatErrorWithRequestId(message: string, requestId?: string): string {
  if (!requestId) return message;
  return `${message} (Ref: ${requestId})`;
}
