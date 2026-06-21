"use client";

import { StatusBadge } from "@/components/admin/StatusBadge";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminHealth } from "@/hooks/admin/useAdmin";

export default function AdminHealthPage() {
  const { data, isLoading } = useAdminHealth();

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  const db = data?.database as Record<string, unknown> | undefined;
  const worker = data?.backgroundWorker as Record<string, unknown> | undefined;

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
