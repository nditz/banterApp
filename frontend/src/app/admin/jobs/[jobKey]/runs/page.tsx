"use client";

import Link from "next/link";
import { use } from "react";
import {
  AdminMobileCard,
  AdminMobileCardRow,
  ResponsiveDataTable,
} from "@/components/admin/ResponsiveDataTable";
import { StatusBadge } from "@/components/admin/StatusBadge";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminJobRuns } from "@/hooks/admin/useAdmin";

export default function JobRunsPage({
  params,
}: {
  params: Promise<{ jobKey: string }>;
}) {
  const { jobKey } = use(params);
  const decodedKey = decodeURIComponent(jobKey);
  const { data: runs, isLoading } = useAdminJobRuns(decodedKey);

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  return (
    <div className="space-y-6">
      <div>
        <Link href="/admin/jobs" className="text-sm text-cyan-400 hover:underline">
          ← Jobs
        </Link>
        <h2 className="mt-2 break-anywhere text-xl font-semibold sm:text-2xl">
          Run history: {decodedKey}
        </h2>
      </div>

      <ResponsiveDataTable
        mobileCards={
          runs?.length === 0 ? (
            <p className="text-center text-zinc-500">No runs recorded yet.</p>
          ) : (
            runs?.map((run) => (
              <AdminMobileCard key={run.runId}>
                <div className="mb-2 flex items-center justify-between gap-2">
                  <StatusBadge status={run.status} />
                  <Link
                    href={`/admin/jobs/${encodeURIComponent(decodedKey)}/runs/${run.runId}`}
                    className="text-sm text-cyan-400 hover:underline"
                  >
                    Detail
                  </Link>
                </div>
                <AdminMobileCardRow label="Started">
                  {new Date(run.startedAt).toLocaleString()}
                </AdminMobileCardRow>
                <AdminMobileCardRow label="Duration">{run.durationMs ?? "—"} ms</AdminMobileCardRow>
                <AdminMobileCardRow label="Processed / Failed">
                  {run.itemsProcessed} / {run.itemsFailed}
                </AdminMobileCardRow>
              </AdminMobileCard>
            ))
          )
        }
      >
        <thead className="border-b border-zinc-800 bg-zinc-900/80 text-xs uppercase text-zinc-500">
          <tr>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Started</th>
            <th className="px-4 py-3">Duration</th>
            <th className="hidden px-4 py-3 md:table-cell">Processed</th>
            <th className="hidden px-4 py-3 md:table-cell">Failed</th>
            <th className="px-4 py-3" />
          </tr>
        </thead>
        <tbody>
          {runs?.length === 0 && (
            <tr>
              <td colSpan={6} className="px-4 py-8 text-center text-zinc-500">
                No runs recorded yet.
              </td>
            </tr>
          )}
          {runs?.map((run) => (
            <tr key={run.runId} className="border-b border-zinc-800/80">
              <td className="px-4 py-3">
                <StatusBadge status={run.status} />
              </td>
              <td className="px-4 py-3 text-xs">{new Date(run.startedAt).toLocaleString()}</td>
              <td className="px-4 py-3 tabular-nums">{run.durationMs ?? "—"} ms</td>
              <td className="hidden px-4 py-3 tabular-nums md:table-cell">{run.itemsProcessed}</td>
              <td className="hidden px-4 py-3 tabular-nums md:table-cell">{run.itemsFailed}</td>
              <td className="px-4 py-3">
                <Link
                  href={`/admin/jobs/${encodeURIComponent(decodedKey)}/runs/${run.runId}`}
                  className="text-cyan-400 hover:underline"
                >
                  Detail
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </ResponsiveDataTable>
    </div>
  );
}
