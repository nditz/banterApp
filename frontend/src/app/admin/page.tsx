"use client";

import { useState } from "react";
import Link from "next/link";
import { ConfirmDialog } from "@/components/admin/ConfirmDialog";
import { StatCard } from "@/components/admin/StatCard";
import { useAdminToast } from "@/components/admin/AdminToast";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useAdminBulkJobAction,
  useAdminJobAction,
  useAdminOverview,
} from "@/hooks/admin/useAdmin";
import { getApiErrorMessage } from "@/lib/api";

export default function AdminOverviewPage() {
  const { data, isLoading, error } = useAdminOverview();
  const jobAction = useAdminJobAction();
  const bulkAction = useAdminBulkJobAction();
  const { showToast } = useAdminToast();
  const [confirmPauseAll, setConfirmPauseAll] = useState(false);

  const run = async (jobKey: string) => {
    try {
      await jobAction.mutateAsync({ jobKey, action: "run" });
      showToast(`Triggered ${jobKey}`);
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  if (isLoading) {
    return <Skeleton className="h-64 w-full" />;
  }

  if (error || !data) {
    return <p className="text-red-400">Failed to load overview.</p>;
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold">Overview</h2>
        <p className="text-sm text-zinc-500">Operational snapshot for ingestion and jobs.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="RSS items" value={data.totalRssItems} />
        <StatCard label="YouTube items" value={data.totalYoutubeItems} />
        <StatCard label="Opinions extracted" value={data.totalOpinions} />
        <StatCard label="Predictions" value={data.totalPredictions} />
        <StatCard label="Needs review" value={data.itemsNeedingReview} />
        <StatCard label="Failed jobs (24h)" value={data.failedJobsLast24h} />
        <StatCard label="OpenAI requests (24h)" value={data.openAiRequestsLast24h} />
        <StatCard
          label="Scheduler"
          value={data.jobsEnabled ? "Active" : "Off"}
          sub={data.openAiConfigured ? "OpenAI configured" : "OpenAI missing"}
        />
      </div>

      <section className="space-y-3">
        <h3 className="text-sm font-medium uppercase tracking-wide text-zinc-500">Quick actions</h3>
        <div className="flex flex-wrap gap-2">
          <Button size="sm" onClick={() => run("rss.sync")}>
            RSS sync
          </Button>
          <Button size="sm" onClick={() => run("youtube.search.sync")}>
            YouTube sync
          </Button>
          <Button size="sm" onClick={() => run("openai.opinion.extract")}>
            OpenAI extraction
          </Button>
          <Button size="sm" onClick={() => run("predictions.aggregate.refresh")}>
            Refresh aggregates
          </Button>
          <Button size="sm" variant="outline" onClick={() => setConfirmPauseAll(true)}>
            Pause all jobs
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={async () => {
              await bulkAction.mutateAsync("resume-all");
              showToast("All jobs resumed");
            }}
          >
            Resume all jobs
          </Button>
        </div>
      </section>

      <section className="grid gap-4 sm:grid-cols-2">
        <div className="rounded-lg border border-zinc-800 p-4 text-sm">
          <p className="text-zinc-500">Latest successful sync</p>
          <p>{data.latestSuccessfulSyncAt ?? "—"}</p>
        </div>
        <div className="rounded-lg border border-zinc-800 p-4 text-sm">
          <p className="text-zinc-500">Latest failed sync</p>
          <p>{data.latestFailedSyncAt ?? "—"}</p>
        </div>
      </section>

      <div className="flex gap-4 text-sm">
        <Link href="/admin/jobs" className="text-cyan-400 hover:underline">
          Jobs console →
        </Link>
        <Link href="/admin/review" className="text-cyan-400 hover:underline">
          Review queue →
        </Link>
      </div>

      <ConfirmDialog
        open={confirmPauseAll}
        onOpenChange={setConfirmPauseAll}
        title="Pause all scheduled jobs?"
        description="Running jobs will finish; future scheduled runs will stop until resumed."
        confirmLabel="Pause all"
        destructive
        onConfirm={async () => {
          await bulkAction.mutateAsync("pause-all");
          showToast("All jobs paused");
        }}
      />
    </div>
  );
}
