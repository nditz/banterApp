"use client";

import { useState } from "react";
import { ConfirmDialog } from "@/components/admin/ConfirmDialog";
import { useAdminToast } from "@/components/admin/AdminToast";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminLaunchChecklist } from "@/hooks/admin/useAdmin";
import { apiFetch, getApiErrorMessage } from "@/lib/api";

export default function LaunchChecklistPage() {
  const { data, isLoading } = useAdminLaunchChecklist();
  const { showToast } = useAdminToast();
  const [confirmBackfill, setConfirmBackfill] = useState<string | null>(null);

  const backfill = async (path: string) => {
    try {
      await apiFetch(path, { method: "POST" });
      showToast("Backfill triggered");
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold">Launch Checklist</h2>
        <p className="text-sm text-zinc-500">Production readiness for v1 hosting.</p>
      </div>

      <ul className="space-y-2">
        {data?.items.map((item) => (
          <li
            key={item.label}
            className="flex items-center gap-3 rounded-md border border-zinc-800 px-4 py-3 text-sm"
          >
            <span className={item.passed ? "text-emerald-400" : "text-red-400"}>
              {item.passed ? "✓" : "✗"}
            </span>
            {item.label}
          </li>
        ))}
      </ul>

      {data?.contentSafety && (
        <section>
          <h3 className="mb-3 text-sm font-medium uppercase text-zinc-500">Content safety</h3>
          <pre className="overflow-x-auto rounded-lg border border-zinc-800 bg-zinc-900 p-4 text-xs">
            {JSON.stringify(data.contentSafety, null, 2)}
          </pre>
        </section>
      )}

      {data?.rateLimits && (
        <section>
          <h3 className="mb-3 text-sm font-medium uppercase text-zinc-500">Rate limits today</h3>
          <pre className="overflow-x-auto rounded-lg border border-zinc-800 bg-zinc-900 p-4 text-xs">
            {JSON.stringify(data.rateLimits, null, 2)}
          </pre>
        </section>
      )}

      <section className="space-y-3">
        <h3 className="text-sm font-medium uppercase text-zinc-500">Backfill tools</h3>
        <div className="flex flex-wrap gap-2">
          <Button size="sm" variant="outline" onClick={() => setConfirmBackfill("/api/admin/backfill/rss")}>
            Backfill RSS
          </Button>
          <Button size="sm" variant="outline" onClick={() => setConfirmBackfill("/api/admin/backfill/youtube")}>
            Backfill YouTube
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => setConfirmBackfill("/api/admin/backfill/failed-extractions")}
          >
            Reprocess failed extractions
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => setConfirmBackfill("/api/admin/backfill/prediction-aggregates")}
          >
            Refresh prediction aggregates
          </Button>
        </div>
      </section>

      <ConfirmDialog
        open={Boolean(confirmBackfill)}
        onOpenChange={(o) => !o && setConfirmBackfill(null)}
        title="Run backfill?"
        description="This may trigger heavy background processing. Continue?"
        confirmLabel="Run backfill"
        destructive
        onConfirm={async () => {
          if (confirmBackfill) await backfill(confirmBackfill);
        }}
      />
    </div>
  );
}
