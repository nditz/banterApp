"use client";

import { StatCard } from "@/components/admin/StatCard";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminStats } from "@/hooks/admin/useAdmin";

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
        <h3 className="text-sm font-medium uppercase text-zinc-500">Product analytics</h3>
        <p className="text-sm text-zinc-500">
          DAU, page views, and feed engagement can be wired via AppMetric when analytics is enabled.
        </p>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {["dailyActiveUsers", "pageViews", "feedImpressions", "feedClicks", "shares"].map((key) => {
            const metric = product[key] as { available?: boolean; metricKey?: string } | undefined;
            return (
              <StatCard
                key={key}
                label={key.replace(/([A-Z])/g, " $1")}
                value={metric?.available ? "Live" : "Not wired"}
                sub={metric?.metricKey}
              />
            );
          })}
        </div>
      </section>
    </div>
  );
}
