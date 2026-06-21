"use client";

import Link from "next/link";
import { use } from "react";
import { StatusBadge } from "@/components/admin/StatusBadge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminJobAction, useAdminJobRunDetail } from "@/hooks/admin/useAdmin";

export default function JobRunDetailPage({
  params,
}: {
  params: Promise<{ jobKey: string; runId: string }>;
}) {
  const { jobKey, runId } = use(params);
  const decodedKey = decodeURIComponent(jobKey);
  const { data, isLoading } = useAdminJobRunDetail(decodedKey, runId);
  const jobAction = useAdminJobAction();

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  if (!data) return <p className="text-red-400">Run not found.</p>;

  const status = String(data.status ?? "unknown");

  return (
    <div className="space-y-6">
      <div>
        <Link
          href={`/admin/jobs/${encodeURIComponent(decodedKey)}/runs`}
          className="text-sm text-cyan-400 hover:underline"
        >
          ← Run history
        </Link>
        <h2 className="mt-2 text-xl font-semibold">Run detail</h2>
        <StatusBadge status={status} />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Info label="Started" value={String(data.startedAt ?? "—")} />
        <Info label="Finished" value={String(data.finishedAt ?? "—")} />
        <Info label="Duration (ms)" value={String(data.durationMs ?? "—")} />
        <Info label="Items processed" value={String(data.itemsProcessed ?? "—")} />
        <Info label="Created" value={String(data.itemsCreated ?? "—")} />
        <Info label="Updated" value={String(data.itemsUpdated ?? "—")} />
        <Info label="Skipped" value={String(data.itemsSkipped ?? "—")} />
        <Info label="Failed" value={String(data.itemsFailed ?? "—")} />
      </div>

      {data.errorMessage ? (
        <div className="rounded-lg border border-red-900/50 bg-red-950/30 p-4 text-sm text-red-200">
          {String(data.errorMessage)}
        </div>
      ) : null}

      {data.metadataJson ? (
        <pre className="overflow-x-auto rounded-lg border border-zinc-800 bg-zinc-900 p-4 text-xs">
          {String(data.metadataJson)}
        </pre>
      ) : null}

      {status === "failed" && (
        <Button
          onClick={() => jobAction.mutate({ jobKey: decodedKey, action: "run" })}
        >
          Retry job
        </Button>
      )}
    </div>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-zinc-800 p-3 text-sm">
      <p className="text-zinc-500">{label}</p>
      <p className="mt-1 break-all">{value}</p>
    </div>
  );
}
