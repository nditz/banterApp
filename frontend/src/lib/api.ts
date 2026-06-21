import { getAnonymousId } from "./anonymous";
import { getCsrfToken } from "./csrf";
import { detectCountryCode } from "./country";
import {
  formatErrorWithRequestId,
  getSafeMessageForCode,
  isApiErrorEnvelope,
  type ApiErrorBody,
  type ApiErrorEnvelope,
} from "./errors";

function resolveApiUrl(): string {
  if (process.env.NEXT_PUBLIC_API_URL) {
    return process.env.NEXT_PUBLIC_API_URL;
  }

  if (typeof window !== "undefined") {
    return "/api-backend";
  }

  return "http://localhost:5000";
}

const API_URL = resolveApiUrl();

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public body?: unknown,
    public code?: string,
    public requestId?: string,
    public details?: Record<string, string[] | string>
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export interface FetchOptions extends RequestInit {
  skipAuth?: boolean;
  requestId?: string;
}

function createRequestId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return `req_${crypto.randomUUID().replace(/-/g, "")}`;
  }

  return `req_${Date.now().toString(36)}`;
}

async function getAuthHeaders(requestId?: string): Promise<Record<string, string>> {
  const headers: Record<string, string> = {
    "X-Request-Id": requestId ?? createRequestId(),
  };

  const anonymousId = getAnonymousId();
  if (anonymousId) {
    headers["X-Anonymous-Id"] = anonymousId;
  }

  const csrf = getCsrfToken();
  if (csrf) {
    headers["X-CSRF-Token"] = csrf;
  }

  if (typeof window !== "undefined") {
    headers["X-Country-Code"] = detectCountryCode();
  }

  if (typeof window !== "undefined") {
    const { createClient } = await import("./supabase/client");
    const supabase = createClient();
    if (supabase) {
      const {
        data: { session },
      } = await supabase.auth.getSession();
      if (session?.access_token) {
        headers.Authorization = `Bearer ${session.access_token}`;
      }
    }
  }

  return headers;
}

function parseApiErrorBody(body: unknown, status: number, responseRequestId?: string | null): ApiError {
  if (isApiErrorEnvelope(body)) {
    const err = body.error;
    const message = getSafeMessageForCode(err.code, err.message);
    return new ApiError(
      message,
      status,
      body,
      err.code,
      err.requestId ?? responseRequestId ?? undefined,
      err.details as Record<string, string[] | string> | undefined
    );
  }

  const legacy = body as { error?: string; title?: string; requestId?: string } | undefined;
  const message =
    legacy?.error ??
    legacy?.title ??
    getSafeMessageForCode("INTERNAL_SERVER_ERROR", `Request failed (${status}).`);

  return new ApiError(
    message,
    status,
    body,
    undefined,
    legacy?.requestId ?? responseRequestId ?? undefined
  );
}

export async function reportClientError(
  error: unknown,
  context: {
    route?: string;
    component?: string;
    metadata?: Record<string, string>;
    requestId?: string;
  } = {}
): Promise<void> {
  if (typeof window === "undefined") {
    return;
  }

  const message = error instanceof Error ? error.message : String(error);
  const stack = error instanceof Error ? error.stack : undefined;

  try {
    await apiFetch<{ success: boolean; requestId?: string }>("/api/errors/client", {
      method: "POST",
      body: JSON.stringify({
        message,
        stack,
        route: context.route ?? window.location.pathname,
        component: context.component,
        userAgent: navigator.userAgent,
        metadata: context.metadata,
        requestId: context.requestId,
      }),
    });
  } catch {
    // Never throw from client error reporting.
  }
}

export async function apiFetch<T>(
  path: string,
  options: FetchOptions = {}
): Promise<T> {
  const { skipAuth, headers: customHeaders, requestId, ...rest } = options;

  const authHeaders = skipAuth ? { "X-Request-Id": requestId ?? createRequestId() } : await getAuthHeaders(requestId);

  const response = await fetch(`${API_URL}${path}`, {
    ...rest,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...authHeaders,
      ...customHeaders,
    },
  });

  const responseRequestId = response.headers.get("X-Request-Id");

  if (!response.ok) {
    let body: unknown;
    try {
      body = await response.json();
    } catch {
      body = await response.text();
    }

    const apiError = parseApiErrorBody(body, response.status, responseRequestId);

    if (response.status >= 500 && typeof window !== "undefined") {
      void reportClientError(apiError, {
        route: window.location.pathname,
        component: "apiFetch",
        requestId: apiError.requestId,
      });
    }

    throw apiError;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export function getApiUrl(): string {
  return API_URL;
}

export function getApiErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const envelope = error.body as ApiErrorEnvelope | undefined;
    if (envelope?.error?.message) {
      return formatErrorWithRequestId(
        getSafeMessageForCode(envelope.error.code, envelope.error.message),
        error.requestId ?? envelope.error.requestId
      );
    }

    const legacy = error.body as { error?: string; title?: string } | undefined;
    if (legacy?.error) {
      return formatErrorWithRequestId(legacy.error, error.requestId);
    }
    if (legacy?.title) {
      return formatErrorWithRequestId(legacy.title, error.requestId);
    }

    if (error.status === 404) {
      return formatErrorWithRequestId(
        "Session API not found — restart the backend (dotnet run in backend/BanterApp.Api).",
        error.requestId
      );
    }

    return formatErrorWithRequestId(`Request failed (${error.status}).`, error.requestId);
  }

  if (error instanceof TypeError) {
    return "Cannot reach the API. Start the backend on port 5000 and refresh.";
  }

  return "Something went wrong. Try again.";
}

export function getApiErrorDetails(error: unknown): ApiErrorBody | undefined {
  if (error instanceof ApiError && isApiErrorEnvelope(error.body)) {
    return error.body.error;
  }

  return undefined;
}
