"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type {
  AdminJob,
  AdminJobRun,
  AdminOverview,
  AdminReviewItem,
  AdminSource,
  AdminSourceItem,
  OperationalErrorDetail,
  OperationalErrorItem,
  LaunchChecklistItem,
} from "@/lib/admin/types";

const adminKey = ["admin"] as const;

export function useAdminOverview() {
  return useQuery({
    queryKey: [...adminKey, "overview"],
    queryFn: () => apiFetch<AdminOverview>("/api/admin/overview"),
  });
}

export function useAdminJobs() {
  return useQuery({
    queryKey: [...adminKey, "jobs"],
    queryFn: () => apiFetch<AdminJob[]>("/api/admin/jobs"),
  });
}

export function useAdminJobRuns(jobKey: string) {
  return useQuery({
    queryKey: [...adminKey, "jobs", jobKey, "runs"],
    queryFn: () => apiFetch<AdminJobRun[]>(`/api/admin/jobs/${encodeURIComponent(jobKey)}/runs`),
    enabled: Boolean(jobKey),
  });
}

export function useAdminJobRunDetail(jobKey: string, runId: string) {
  return useQuery({
    queryKey: [...adminKey, "jobs", jobKey, "runs", runId],
    queryFn: () =>
      apiFetch<Record<string, unknown>>(
        `/api/admin/jobs/${encodeURIComponent(jobKey)}/runs/${runId}`
      ),
    enabled: Boolean(jobKey && runId),
  });
}

export function useAdminErrors(filters?: {
  status?: string;
  severity?: string;
  source?: string;
  provider?: string;
  search?: string;
}) {
  const search = new URLSearchParams();
  if (filters?.status) search.set("status", filters.status);
  if (filters?.severity) search.set("severity", filters.severity);
  if (filters?.source) search.set("source", filters.source);
  if (filters?.provider) search.set("provider", filters.provider);
  if (filters?.search) search.set("search", filters.search);
  const qs = search.toString();

  return useQuery({
    queryKey: [...adminKey, "errors", filters ?? "all"],
    queryFn: () =>
      apiFetch<OperationalErrorItem[]>(`/api/admin/errors${qs ? `?${qs}` : ""}`),
  });
}

export function useAdminErrorDetail(id: string) {
  return useQuery({
    queryKey: [...adminKey, "errors", id],
    queryFn: () => apiFetch<OperationalErrorDetail>(`/api/admin/errors/${id}`),
    enabled: Boolean(id),
  });
}

export function useAdminSources() {
  return useQuery({
    queryKey: [...adminKey, "sources"],
    queryFn: () => apiFetch<AdminSource[]>("/api/admin/sources"),
  });
}

export function useAdminSourceItems(params?: {
  sourceId?: string;
  status?: string;
  needsReview?: boolean;
}) {
  const search = new URLSearchParams();
  if (params?.sourceId) search.set("sourceId", params.sourceId);
  if (params?.status) search.set("status", params.status);
  if (params?.needsReview) search.set("needsReview", "true");
  const qs = search.toString();

  return useQuery({
    queryKey: [...adminKey, "source-items", params],
    queryFn: () =>
      apiFetch<AdminSourceItem[]>(`/api/admin/source-items${qs ? `?${qs}` : ""}`),
  });
}

export function useAdminReview() {
  return useQuery({
    queryKey: [...adminKey, "review"],
    queryFn: () => apiFetch<AdminReviewItem[]>("/api/admin/review"),
  });
}

export function useAdminStats() {
  return useQuery({
    queryKey: [...adminKey, "stats"],
    queryFn: () => apiFetch<Record<string, unknown>>("/api/admin/stats"),
  });
}

export function useAdminHealth() {
  return useQuery({
    queryKey: [...adminKey, "health"],
    queryFn: () => apiFetch<Record<string, unknown>>("/api/admin/health"),
  });
}

export function useAdminLaunchChecklist() {
  return useQuery({
    queryKey: [...adminKey, "launch-checklist"],
    queryFn: () =>
      apiFetch<{
        items: LaunchChecklistItem[];
        contentSafety: Record<string, unknown>;
        rateLimits: Record<string, unknown>;
      }>("/api/admin/launch-checklist"),
  });
}

function useAdminMutation<T = unknown>(path: string, invalidateKeys: readonly unknown[] = adminKey) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body?: unknown) =>
      apiFetch<T>(path, {
        method: "POST",
        body: body ? JSON.stringify(body) : undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: invalidateKeys as unknown[] });
    },
  });
}

export function useAdminJobAction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ jobKey, action }: { jobKey: string; action: string }) =>
      apiFetch(`/api/admin/jobs/${encodeURIComponent(jobKey)}/${action}`, { method: "POST" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...adminKey] });
    },
  });
}

export function useAdminBulkJobAction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (action: "pause-all" | "resume-all") =>
      apiFetch(`/api/admin/jobs/${action}`, { method: "POST" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...adminKey] });
    },
  });
}

export function useAdminErrorAction() {
  return useMutation({
    mutationFn: ({
      id,
      action,
    }: {
      id: string;
      action: "resolve" | "ignore" | "retry" | "investigate";
    }) => apiFetch(`/api/admin/errors/${id}/${action}`, { method: "POST" }),
  });
}

export function useAdminSourceAction() {
  return useMutation({
    mutationFn: ({ id, action }: { id: string; action: "sync" | "enable" | "disable" }) =>
      apiFetch(`/api/admin/sources/${id}/${action}`, { method: "POST" }),
  });
}

export function useAdminReviewAction() {
  return useMutation({
    mutationFn: ({
      id,
      action,
      body,
    }: {
      id: string;
      action: "approve" | "reject" | "update";
      body?: unknown;
    }) =>
      apiFetch(`/api/admin/review/${id}/${action}`, {
        method: "POST",
        body: body ? JSON.stringify(body) : undefined,
      }),
  });
}

export function useAdminBackfill() {
  return useAdminMutation("/api/admin/backfill/rss");
}

export function useAdminReprocessItem() {
  return useMutation({
    mutationFn: (id: string) =>
      apiFetch(`/api/admin/source-items/${id}/reprocess`, { method: "POST" }),
  });
}
