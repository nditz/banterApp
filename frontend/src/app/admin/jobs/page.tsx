"use client";

import Link from "next/link";
import { useState } from "react";
import { ConfirmDialog } from "@/components/admin/ConfirmDialog";
import {
  AdminMobileCard,
  AdminMobileCardRow,
  ResponsiveDataTable,
} from "@/components/admin/ResponsiveDataTable";
import { StatusBadge } from "@/components/admin/StatusBadge";
import { useAdminToast } from "@/components/admin/AdminToast";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminJobAction, useAdminJobs } from "@/hooks/admin/useAdmin";
import { getApiErrorMessage } from "@/lib/api";
import type { AdminJob } from "@/lib/admin/types";

function JobActions({
  job,
  onAct,
  onPause,
}: {
  job: AdminJob;
  onAct: (job: AdminJob, action: string) => void;
  onPause: (job: AdminJob) => void;
}) {
  return (
    <div className="flex flex-wrap gap-1">
      {job.canRunManually && (
        <Button size="xs" variant="outline" onClick={() => onAct(job, "run")}>
          Run
        </Button>
      )}
      {job.canPause && !job.paused && (
        <Button size="xs" variant="outline" onClick={() => onPause(job)}>
          Pause
        </Button>
      )}
      {job.paused && (
        <Button size="xs" variant="outline" onClick={() => onAct(job, "resume")}>
          Resume
        </Button>
      )}
      <Link href={`/admin/jobs/${encodeURIComponent(job.jobKey)}/runs`}>
        <Button size="xs" variant="ghost">
          History
        </Button>
      </Link>
    </div>
  );
}

export default function AdminJobsPage() {
  const { data: jobs, isLoading } = useAdminJobs();
  const jobAction = useAdminJobAction();
  const { showToast } = useAdminToast();
  const [pending, setPending] = useState<{ job: AdminJob; action: string } | null>(null);

  const act = async (job: AdminJob, action: string) => {
    try {
      await jobAction.mutateAsync({ jobKey: job.jobKey, action });
      showToast(`${action} ${job.displayName}`);
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold sm:text-2xl">Jobs Console</h2>
        <p className="text-sm text-zinc-500">Registered background jobs and controls.</p>
      </div>

      <ResponsiveDataTable
        minWidth="640px"
        mobileCards={jobs?.map((job) => (
          <AdminMobileCard key={job.jobKey}>
            <div className="mb-3 flex flex-wrap items-start justify-between gap-2">
              <div className="min-w-0">
                <p className="font-medium">{job.displayName}</p>
                <p className="break-anywhere text-xs text-zinc-500">{job.jobKey}</p>
              </div>
              <StatusBadge status={job.status} />
            </div>
            <AdminMobileCardRow label="Last run">
              {job.lastRunAt ? new Date(job.lastRunAt).toLocaleString() : "—"}
            </AdminMobileCardRow>
            <AdminMobileCardRow label="Success / Fail">
              {job.successCount} / {job.failureCount}
            </AdminMobileCardRow>
            <div className="mt-3">
              <JobActions job={job} onAct={act} onPause={(j) => setPending({ job: j, action: "pause" })} />
            </div>
          </AdminMobileCard>
        ))}
      >
        <thead className="border-b border-zinc-800 bg-zinc-900/80 text-xs uppercase text-zinc-500">
          <tr>
            <th className="px-4 py-3">Job</th>
            <th className="px-4 py-3">Status</th>
            <th className="hidden px-4 py-3 md:table-cell">Schedule</th>
            <th className="px-4 py-3">Last run</th>
            <th className="hidden px-4 py-3 lg:table-cell">Success / Fail</th>
            <th className="px-4 py-3">Actions</th>
          </tr>
        </thead>
        <tbody>
          {jobs?.map((job) => (
            <tr key={job.jobKey} className="border-b border-zinc-800/80">
              <td className="px-4 py-3">
                <p className="font-medium">{job.displayName}</p>
                <p className="break-anywhere text-xs text-zinc-500">{job.jobKey}</p>
              </td>
              <td className="px-4 py-3">
                <StatusBadge status={job.status} />
              </td>
              <td className="hidden px-4 py-3 font-mono text-xs md:table-cell">{job.schedule ?? "—"}</td>
              <td className="px-4 py-3 text-xs text-zinc-400">
                {job.lastRunAt ? new Date(job.lastRunAt).toLocaleString() : "—"}
              </td>
              <td className="hidden px-4 py-3 tabular-nums lg:table-cell">
                {job.successCount} / {job.failureCount}
              </td>
              <td className="px-4 py-3">
                <JobActions job={job} onAct={act} onPause={(j) => setPending({ job: j, action: "pause" })} />
              </td>
            </tr>
          ))}
        </tbody>
      </ResponsiveDataTable>

      <ConfirmDialog
        open={Boolean(pending)}
        onOpenChange={(o) => !o && setPending(null)}
        title={`${pending?.action} job?`}
        description={`Stop future scheduled runs for ${pending?.job.displayName}?`}
        confirmLabel="Confirm"
        onConfirm={async () => {
          if (pending) await act(pending.job, pending.action);
        }}
      />
    </div>
  );
}
