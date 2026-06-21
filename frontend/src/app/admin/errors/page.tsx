"use client";

import Link from "next/link";
import { useState } from "react";
import { StatusBadge } from "@/components/admin/StatusBadge";
import { useAdminToast } from "@/components/admin/AdminToast";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminErrorAction, useAdminErrors } from "@/hooks/admin/useAdmin";
import { getApiErrorMessage } from "@/lib/api";
import { cn } from "@/lib/utils";

export default function AdminErrorsPage() {
  const [status, setStatus] = useState<string>("");
  const [severity, setSeverity] = useState<string>("");
  const [source, setSource] = useState<string>("");
  const [provider, setProvider] = useState<string>("");
  const [search, setSearch] = useState<string>("");

  const { data: errors, isLoading, isError, refetch } = useAdminErrors({
    status: status || undefined,
    severity: severity || undefined,
    source: source || undefined,
    provider: provider || undefined,
    search: search || undefined,
  });
  const errorAction = useAdminErrorAction();
  const { showToast } = useAdminToast();

  const act = async (id: string, action: "resolve" | "ignore" | "retry" | "investigate") => {
    try {
      await errorAction.mutateAsync({ id, action });
      showToast(`Error ${action}${action.endsWith("e") ? "d" : "ed"}`);
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold">Error Management</h2>
          <p className="text-sm text-zinc-500">Grouped operational errors across API, jobs, and frontend.</p>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <Input
          className="w-full"
          placeholder="Search code / request ID"
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          className="w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-2.5 text-sm"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
        >
          <option value="">All statuses</option>
          <option value="open">Open</option>
          <option value="investigating">Investigating</option>
          <option value="resolved">Resolved</option>
          <option value="ignored">Ignored</option>
          <option value="retry_scheduled">Retry scheduled</option>
        </select>
        <select
          className="w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-2.5 text-sm"
          value={severity}
          onChange={(e) => setSeverity(e.target.value)}
        >
          <option value="">All severities</option>
          <option value="info">Info</option>
          <option value="warning">Warning</option>
          <option value="error">Error</option>
          <option value="critical">Critical</option>
        </select>
        <select
          className="w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-2.5 text-sm"
          value={source}
          onChange={(e) => setSource(e.target.value)}
        >
          <option value="">All sources</option>
          <option value="backend">Backend</option>
          <option value="frontend">Frontend</option>
          <option value="job">Job</option>
          <option value="provider">Provider</option>
        </select>
        <select
          className="w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-2.5 text-sm"
          value={provider}
          onChange={(e) => setProvider(e.target.value)}
        >
          <option value="">All providers</option>
          <option value="openai">OpenAI</option>
          <option value="youtube">YouTube</option>
          <option value="rss">RSS</option>
          <option value="database">Database</option>
          <option value="app">App</option>
        </select>
      </div>

      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : isError ? (
        <p className="text-destructive">Failed to load errors.</p>
      ) : (
        <div className="space-y-3">
          {errors?.length === 0 && (
            <p className="text-zinc-500">No errors found. Systems look clean.</p>
          )}
          {errors?.map((err) => (
            <div key={err.id} className="rounded-lg border border-zinc-800 p-4">
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <StatusBadge status={err.status} />
                    <span className="text-xs text-zinc-500">{err.severity}</span>
                    <span className="text-xs font-mono text-zinc-600">{err.errorCode}</span>
                    <span className="text-xs text-zinc-600">×{err.count}</span>
                  </div>
                  <p className="mt-2 font-medium break-words">{err.message}</p>
                  <p className="mt-1 break-anywhere text-xs text-zinc-500">
                    {err.source}
                    {err.jobKey ? ` · ${err.jobKey}` : ""}
                    {err.provider ? ` · ${err.provider}` : ""}
                    {" · last "}
                    {new Date(err.lastSeenAt).toLocaleString()}
                    {err.requestId ? ` · ${err.requestId}` : ""}
                  </p>
                </div>
                <div className="flex flex-wrap gap-1">
                  <Link
                    href={`/admin/errors/${err.id}`}
                    className={cn(buttonVariants({ size: "xs", variant: "outline" }))}
                  >
                    Details
                  </Link>
                  {err.status === "open" && (
                    <>
                      <Button size="xs" variant="outline" onClick={() => act(err.id, "investigate")}>
                        Investigate
                      </Button>
                      <Button size="xs" variant="outline" onClick={() => act(err.id, "resolve")}>
                        Resolve
                      </Button>
                      <Button size="xs" variant="outline" onClick={() => act(err.id, "ignore")}>
                        Ignore
                      </Button>
                      <Button size="xs" onClick={() => act(err.id, "retry")}>
                        Retry
                      </Button>
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
