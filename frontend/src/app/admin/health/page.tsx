"use client";

import { StatusBadge } from "@/components/admin/StatusBadge";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminHealth } from "@/hooks/admin/useAdmin";

export default function AdminHealthPage() {
  const { data, isLoading } = useAdminHealth();

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  const db = data?.database as Record<string, unknown> | undefined;
  const worker = data?.backgroundWorker as Record<string, unknown> | undefined;
  const pundit = data?.punditPipeline as
    | {
        aiProvider?: string;
        usingOpenAiExtractor?: boolean;
        mediaItems?: Record<string, number>;
        opinions?: Record<string, number>;
      }
    | undefined;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold">System Health</h2>
        <p className="text-sm text-zinc-500">Connectivity and environment status (no secrets shown).</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <HealthCard label="Database" ok={Boolean(db?.connected)} detail={String(db?.provider ?? "")} />
        <HealthCard
          label="Background worker"
          ok={Boolean(worker?.active)}
          detail={String(data?.environmentName ?? "")}
        />
        <HealthCard
          label="OpenAI"
          ok={Boolean((data?.openAi as Record<string, unknown>)?.configured)}
        />
        <HealthCard
          label="YouTube"
          ok={Boolean((data?.youtube as Record<string, unknown>)?.configured)}
        />
        <HealthCard label="RSS feeds" ok={Boolean((data?.rss as Record<string, unknown>)?.reachable)} />
        <HealthCard label="Storage" ok={(data?.storage as Record<string, unknown>)?.status === "ok"} />
      </div>

      {pundit ? (
        <div className="rounded-lg border border-zinc-800 p-4 text-sm">
          <div className="flex items-center justify-between">
            <p className="font-medium">Pundit pipeline</p>
            <StatusBadge
              status={pundit.usingOpenAiExtractor ? "success" : "failed"}
            />
          </div>
          <p className="mt-1 text-xs text-zinc-500">
            Extractor:{" "}
            <span className="font-mono">
              {pundit.usingOpenAiExtractor ? "OpenAI" : "stub (placeholders only)"}
            </span>
          </p>

          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs uppercase tracking-wide text-zinc-500">
                Source items by status
              </p>
              <dl className="mt-1 space-y-0.5">
                {["pending", "enriched", "extracted", "failed", "skipped"].map((k) => (
                  <div key={k} className="flex justify-between">
                    <dt className="capitalize text-zinc-400">{k}</dt>
                    <dd className="font-mono">{pundit.mediaItems?.[k] ?? 0}</dd>
                  </div>
                ))}
              </dl>
            </div>
            <div>
              <p className="text-xs uppercase tracking-wide text-zinc-500">
                Extracted opinions
              </p>
              <dl className="mt-1 space-y-0.5">
                <div className="flex justify-between">
                  <dt className="text-zinc-400">Total</dt>
                  <dd className="font-mono">{pundit.opinions?.total ?? 0}</dd>
                </div>
                <div className="flex justify-between">
                  <dt className="text-zinc-400">Visible in feed</dt>
                  <dd className="font-mono">{pundit.opinions?.visibleInFeed ?? 0}</dd>
                </div>
                <div className="flex justify-between">
                  <dt className="text-zinc-400">Awaiting review</dt>
                  <dd className="font-mono">{pundit.opinions?.needingReview ?? 0}</dd>
                </div>
                <div className="flex justify-between">
                  <dt className="text-zinc-400">Rejected</dt>
                  <dd className="font-mono">{pundit.opinions?.rejected ?? 0}</dd>
                </div>
              </dl>
            </div>
          </div>
        </div>
      ) : null}

      <div className="rounded-lg border border-zinc-800 p-4 text-sm">
        <p className="text-zinc-500">Last successful cron run</p>
        <p>{String(data?.lastSuccessfulCronRun ?? "—")}</p>
        <p className="mt-2 text-zinc-500">Git commit</p>
        <p className="font-mono text-xs">{String(data?.gitCommit ?? "not set")}</p>
      </div>
    </div>
  );
}

function HealthCard({
  label,
  ok,
  detail,
}: {
  label: string;
  ok: boolean;
  detail?: string;
}) {
  return (
    <div className="rounded-lg border border-zinc-800 p-4">
      <div className="flex items-center justify-between">
        <p className="font-medium">{label}</p>
        <StatusBadge status={ok ? "success" : "failed"} />
      </div>
      {detail ? <p className="mt-2 text-xs text-zinc-500">{detail}</p> : null}
    </div>
  );
}
