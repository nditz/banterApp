"use client";

import Link from "next/link";
import { use } from "react";
import { StatusBadge } from "@/components/admin/StatusBadge";
import { useAdminToast } from "@/components/admin/AdminToast";
import { Button, buttonVariants } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminErrorAction, useAdminErrorDetail } from "@/hooks/admin/useAdmin";
import { getApiErrorMessage } from "@/lib/api";
import { cn } from "@/lib/utils";

export default function AdminErrorDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { data: error, isLoading, isError, refetch } = useAdminErrorDetail(id);
  const errorAction = useAdminErrorAction();
  const { showToast } = useAdminToast();

  const act = async (action: "resolve" | "ignore" | "retry" | "investigate") => {
    try {
      await errorAction.mutateAsync({ id, action });
      showToast(`Error ${action}${action.endsWith("e") ? "d" : "ed"}`);
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  if (isLoading) {
    return <Skeleton className="h-96 w-full" />;
  }

  if (isError || !error) {
    return (
      <div className="space-y-4">
        <p className="text-destructive">Failed to load error details.</p>
        <Link href="/admin/errors" className={cn(buttonVariants({ variant: "outline" }))}>
          Back to errors
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Link href="/admin/errors" className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
            ← Back
          </Link>
          <h2 className="mt-3 text-xl font-semibold">{error.message}</h2>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            <StatusBadge status={error.status} />
            <span className="text-xs text-zinc-500">{error.severity}</span>
            <span className="font-mono text-xs text-zinc-600">{error.errorCode}</span>
            <span className="text-xs text-zinc-600">×{error.count}</span>
          </div>
        </div>
        <div className="flex flex-wrap gap-1">
          {error.status === "open" && (
            <>
              <Button size="sm" variant="outline" onClick={() => act("investigate")}>
                Investigate
              </Button>
              <Button size="sm" variant="outline" onClick={() => act("resolve")}>
                Resolve
              </Button>
              <Button size="sm" variant="outline" onClick={() => act("ignore")}>
                Ignore
              </Button>
              <Button size="sm" onClick={() => act("retry")}>
                Retry
              </Button>
            </>
          )}
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <DetailCard title="Summary">
          <DetailRow label="Request ID" value={error.requestId} />
          <DetailRow label="Source" value={error.source} />
          <DetailRow label="Provider" value={error.provider} />
          <DetailRow label="Job key" value={error.jobKey} />
          <DetailRow label="Route" value={error.route} />
          <DetailRow label="First seen" value={new Date(error.firstSeenAt).toLocaleString()} />
          <DetailRow label="Last seen" value={new Date(error.lastSeenAt).toLocaleString()} />
        </DetailCard>
        <DetailCard title="Internal (sanitized)">
          <p className="text-sm text-zinc-300">{error.messageInternal ?? "—"}</p>
          {error.detailAvailable && error.stackTrace && (
            <pre className="mt-3 max-h-64 overflow-auto rounded bg-zinc-950 p-3 text-xs text-zinc-400">
              {error.stackTrace}
            </pre>
          )}
          {!error.detailAvailable && (
            <p className="mt-2 text-xs text-zinc-500">Enable ExposeErrorDetail for stack traces.</p>
          )}
        </DetailCard>
      </div>

      {error.metadataJson && (
        <DetailCard title="Metadata">
          <pre className="max-h-64 overflow-auto rounded bg-zinc-950 p-3 text-xs text-zinc-400">
            {error.metadataJson}
          </pre>
        </DetailCard>
      )}
    </div>
  );
}

function DetailCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-zinc-800 p-4">
      <h3 className="mb-3 text-sm font-semibold text-zinc-300">{title}</h3>
      {children}
    </div>
  );
}

function DetailRow({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div className="flex flex-col gap-1 border-b border-zinc-900 py-2 text-sm last:border-0 sm:flex-row sm:justify-between sm:gap-4">
      <span className="text-zinc-500">{label}</span>
      <span className="break-all text-zinc-200 sm:text-right">{value ?? "—"}</span>
    </div>
  );
}
