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
        predictions?: Record<string, number>;
      }
    | undefined;

  const alerts = (data?.alerts ?? []) as AdminAlert[];
  const receipts = data?.receipts as Record<string, number> | undefined;
  const studio = data?.studio as Record<string, number> | undefined;
  const ads = data?.ads as { consentModel?: string; initFailuresLast24h?: number } | undefined;
  const status = String(data?.status ?? "ok");

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold">System Health</h2>
        <p className="text-sm text-zinc-500">Connectivity and environment status (no secrets shown).</p>
      </div>

      <section className="rounded-lg border border-zinc-800 p-4">
        <div className="flex items-center justify-between">
          <p className="font-medium">
            Active alerts{alerts.length > 0 ? ` (${alerts.length})` : ""}
          </p>
          <StatusBadge status={status === "ok" ? "success" : "failed"} />
        </div>
        {alerts.length === 0 ? (
          <p className="mt-2 text-sm text-zinc-500">
            Nothing needs attention. All health checks are passing.
          </p>
        ) : (
          <ul className="mt-3 space-y-2">
            {alerts.map((alert) => (
              <li
                key={alert.key}
                className="flex items-start gap-2 rounded-md border border-zinc-800 px-3 py-2 text-sm"
              >
                <span
                  className={`mt-0.5 rounded px-1.5 py-0.5 text-[10px] font-semibold uppercase ${severityClass(alert.severity)}`}
                >
                  {alert.severity}
                </span>
                <span>{alert.message}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

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
                <div className="flex justify-between">
                  <dt className="text-zinc-400">Match-linked</dt>
                  <dd className="font-mono">{pundit.opinions?.matchLinked ?? 0}</dd>
                </div>
                <div className="flex justify-between">
                  <dt className="text-zinc-400">Linked, no pick row</dt>
                  <dd className="font-mono">{pundit.opinions?.matchLinkedWithoutPrediction ?? 0}</dd>
                </div>
              </dl>
            </div>
            <div>
              <p className="text-xs uppercase tracking-wide text-zinc-500">
                Match-linked predictions
              </p>
              <dl className="mt-1 space-y-0.5">
                <div className="flex justify-between">
                  <dt className="text-zinc-400">Total</dt>
                  <dd className="font-mono">{pundit.predictions?.total ?? 0}</dd>
                </div>
                <div className="flex justify-between">
                  <dt className="text-zinc-400">Match-linked</dt>
                  <dd className="font-mono">{pundit.predictions?.matchLinked ?? 0}</dd>
                </div>
              </dl>
            </div>
          </div>
        </div>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-lg border border-zinc-800 p-4 text-sm">
          <p className="font-medium">Receipts</p>
          <dl className="mt-2 space-y-0.5">
            <Row label="Total" value={receipts?.total} />
            <Row label="Settled (24h)" value={receipts?.settledLast24h} />
            <Row label="Awaiting settlement" value={receipts?.awaitingSettlement} />
          </dl>
        </div>
        <div className="rounded-lg border border-zinc-800 p-4 text-sm">
          <p className="font-medium">Studio</p>
          <dl className="mt-2 space-y-0.5">
            <Row label="Content packs" value={studio?.contentPacks} />
            <Row label="Packs (24h)" value={studio?.contentPacksLast24h} />
          </dl>
        </div>
        <div className="rounded-lg border border-zinc-800 p-4 text-sm">
          <p className="font-medium">Ads</p>
          <dl className="mt-2 space-y-0.5">
            <div className="flex justify-between">
              <dt className="text-zinc-400">Consent model</dt>
              <dd className="font-mono">{ads?.consentModel ?? "—"}</dd>
            </div>
            <Row label="Init failures (24h)" value={ads?.initFailuresLast24h} />
          </dl>
        </div>
      </div>

      <div className="rounded-lg border border-zinc-800 p-4 text-sm">
        <p className="text-zinc-500">Current matchweek</p>
        <p>{String(data?.currentMatchweek ?? "none")}</p>
        <p className="mt-2 text-zinc-500">Last successful cron run</p>
        <p>{String(data?.lastSuccessfulCronRun ?? "—")}</p>
        <p className="mt-2 text-zinc-500">Git commit</p>
        <p className="font-mono text-xs">{String(data?.gitCommit ?? "not set")}</p>
      </div>
    </div>
  );
}

interface AdminAlert {
  key: string;
  severity: string;
  message: string;
}

function severityClass(severity: string): string {
  if (severity === "critical") return "bg-red-500/15 text-red-400";
  if (severity === "warning") return "bg-amber-500/15 text-amber-400";
  return "bg-zinc-500/15 text-zinc-400";
}

function Row({ label, value }: { label: string; value?: number }) {
  return (
    <div className="flex justify-between">
      <dt className="text-zinc-400">{label}</dt>
      <dd className="font-mono">{value ?? 0}</dd>
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
