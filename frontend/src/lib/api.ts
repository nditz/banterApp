import { getAnonymousId } from "./anonymous";
import { getCsrfToken } from "./csrf";
import { detectCountryCode } from "./country";

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
    public body?: unknown
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export interface FetchOptions extends RequestInit {
  skipAuth?: boolean;
}

async function getAuthHeaders(): Promise<Record<string, string>> {
  const headers: Record<string, string> = {};

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

export async function apiFetch<T>(
  path: string,
  options: FetchOptions = {}
): Promise<T> {
  const { skipAuth, headers: customHeaders, ...rest } = options;

  const authHeaders = skipAuth ? {} : await getAuthHeaders();

  const response = await fetch(`${API_URL}${path}`, {
    ...rest,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...authHeaders,
      ...customHeaders,
    },
  });

  if (!response.ok) {
    let body: unknown;
    try {
      body = await response.json();
    } catch {
      body = await response.text();
    }
    throw new ApiError(
      `API request failed: ${response.status} ${response.statusText}`,
      response.status,
      body
    );
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
    const body = error.body as { error?: string; title?: string } | undefined;
    if (body?.error) return body.error;
    if (body?.title) return body.title;
    if (error.status === 404) {
      return "Session API not found — restart the backend (dotnet run in backend/BanterApp.Api).";
    }
    return `Request failed (${error.status}).`;
  }

  if (error instanceof TypeError) {
    return "Cannot reach the API. Start the backend on port 5000 and refresh.";
  }

  return "Something went wrong. Try again.";
}
