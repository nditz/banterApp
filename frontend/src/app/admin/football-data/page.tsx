"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useAdminFootballOverview,
  useAdminFootballSync,
} from "@/hooks/admin/useAdmin";
import { useAdminToast } from "@/components/admin/AdminToast";
import { getApiErrorMessage } from "@/lib/api";

export default function AdminFootballDataPage() {
  const { data, isLoading, refetch } = useAdminFootballOverview();
  const sync = useAdminFootballSync();
  const { showToast } = useAdminToast();

  const trigger = async (target: "countries" | "players" | "top-scorers" | "top-assists" | "all") => {
    try {
      await sync.mutateAsync(target);
      showToast(`Triggered ${target} sync`);
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold sm:text-2xl">Football reference data</h2>
        <p className="text-sm text-zinc-500">
          Synced countries, players, stats, and leaderboards from {data?.currentProvider ?? "—"}.
        </p>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {[
          ["Countries", data?.countriesCount],
          ["Players", data?.playersCount],
          ["Stats rows", data?.statsCount],
          ["Leaderboard rows", data?.leaderboardEntriesCount],
        ].map(([label, count]) => (
          <div key={label as string} className="rounded-lg border border-zinc-800 bg-zinc-900/50 p-4">
            <p className="text-xs text-zinc-500">{label}</p>
            <p className="text-2xl font-semibold">{count ?? 0}</p>
          </div>
        ))}
      </div>

      <div className="rounded-lg border border-zinc-800 bg-zinc-900/50 p-4 text-sm">
        <p>
          <span className="text-zinc-500">Competition:</span> {data?.competition} · {data?.season}
        </p>
        <p className="mt-1">
          <span className="text-zinc-500">Last sync:</span>{" "}
          {data?.lastSyncAt ? new Date(data.lastSyncAt).toLocaleString() : "Never"}
        </p>
        <p className="mt-1">
          <span className="text-zinc-500">Failed syncs (recent):</span> {data?.failedSyncCount ?? 0}
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        <Button size="sm" onClick={() => trigger("countries")} disabled={sync.isPending}>
          Sync countries
        </Button>
        <Button size="sm" onClick={() => trigger("players")} disabled={sync.isPending}>
          Sync players
        </Button>
        <Button size="sm" onClick={() => trigger("top-scorers")} disabled={sync.isPending}>
          Sync top scorers
        </Button>
        <Button size="sm" onClick={() => trigger("top-assists")} disabled={sync.isPending}>
          Sync top assists
        </Button>
        <Button size="sm" variant="outline" onClick={() => trigger("all")} disabled={sync.isPending}>
          Sync all
        </Button>
      </div>

      <div className="flex flex-wrap gap-3 text-sm">
        <Link href="/admin/football-data/countries" className="text-sky-400 hover:underline">
          Browse countries →
        </Link>
        <Link href="/admin/football-data/players" className="text-sky-400 hover:underline">
          Browse players →
        </Link>
        <Link href="/admin/football-data/leaderboards" className="text-sky-400 hover:underline">
          Browse leaderboards →
        </Link>
      </div>
    </div>
  );
}
