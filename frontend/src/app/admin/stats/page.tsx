"use client";

import { StatCard } from "@/components/admin/StatCard";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminStats } from "@/hooks/admin/useAdmin";

interface FunnelMetric {
  available?: boolean;
  metricKey?: string;
  value?: number;
}

const FUNNEL_METRICS = [
  { key: "predictionsMade", label: "Predictions made" },
  { key: "returnedAfterResult", label: "Returned after result" },
  { key: "receiptViews", label: "Receipt views" },
  { key: "studioOpens", label: "Studio opens" },
  { key: "contentGenerations", label: "Content generations" },
  { key: "contentExports", label: "Copy / export actions" },
  { key: "leagueJoins", label: "League joins" },
  { key: "punditFollows", label: "Pundit follows" },
  { key: "adInitFailures", label: "AdSense init failures" },
  { key: "adSlotsFilled", label: "Ad slots filled" },
  { key: "adSlotsUnfilled", label: "Ad slots with no fill" },
] as const;

export default function AdminStatsPage() {
  const { data, isLoading } = useAdminStats();

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  const product = (data?.product ?? {}) as Record<string, unknown>;
  const backend = (data?.backend ?? {}) as Record<string, unknown>;

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold">App Stats</h2>
        <p className="text-sm text-zinc-500">Product and backend operational metrics.</p>
      </div>

      <section className="space-y-3">
        <h3 className="text-sm font-medium uppercase text-zinc-500">Backend</h3>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard label="Total users" value={String(product.totalUsers ?? "—")} />
          <StatCard label="Queue depth" value={String(backend.queueDepth ?? "—")} />
          <StatCard label="Failed queue items" value={String(backend.failedQueueItems ?? "—")} />
          <StatCard label="API errors today" value={String(backend.apiErrorRateToday ?? "—")} />
          <StatCard label="RSS fetched today" value={String(backend.rssItemsFetchedToday ?? "—")} />
          <StatCard label="YouTube fetched today" value={String(backend.youtubeVideosFetchedToday ?? "—")} />
        </div>
      </section>

      <section className="space-y-3">
        <h3 className="text-sm font-medium uppercase text-zinc-500">Product funnel (last 24h)</h3>
        <p className="text-sm text-zinc-500">
          Anonymous counters recorded through AppMetric. No per-user tracking is stored.
        </p>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {FUNNEL_METRICS.map(({ key, label }) => {
            const metric = product[key] as FunnelMetric | undefined;
            return (
              <StatCard
                key={key}
                label={label}
                value={metric?.available ? String(metric.value ?? 0) : "No data yet"}
                sub={metric?.metricKey}
              />
            );
          })}
        </div>
      </section>
    </div>
  );
}
