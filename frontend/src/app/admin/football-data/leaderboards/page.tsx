"use client";

import { useState } from "react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ResponsiveDataTable } from "@/components/admin/ResponsiveDataTable";
import {
  useAdminFootballLeaderboards,
  useAdminFootballSync,
} from "@/hooks/admin/useAdmin";
import { useAdminToast } from "@/components/admin/AdminToast";
import { getApiErrorMessage } from "@/lib/api";

export default function AdminFootballLeaderboardsPage() {
  const [type, setType] = useState<"top_scorers" | "top_assists">("top_scorers");
  const { data, isLoading, refetch } = useAdminFootballLeaderboards(type);
  const sync = useAdminFootballSync();
  const { showToast } = useAdminToast();

  const refresh = async () => {
    try {
      await sync.mutateAsync(type === "top_scorers" ? "top-scorers" : "top-assists");
      showToast("Leaderboard sync triggered");
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  const entries = data?.entries ?? [];

  return (
    <div className="space-y-4">
      <Link href="/admin/football-data" className="text-sm text-sky-400 hover:underline">
        ← Football data
      </Link>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xl font-semibold">Leaderboards</h2>
        <Button size="sm" onClick={refresh} disabled={sync.isPending}>
          Refresh
        </Button>
      </div>
      <div className="flex gap-2">
        <Button
          size="sm"
          variant={type === "top_scorers" ? "default" : "outline"}
          onClick={() => setType("top_scorers")}
        >
          Top scorers
        </Button>
        <Button
          size="sm"
          variant={type === "top_assists" ? "default" : "outline"}
          onClick={() => setType("top_assists")}
        >
          Top assists
        </Button>
      </div>
      {isLoading ? (
        <Skeleton className="h-48 w-full" />
      ) : (
        <ResponsiveDataTable>
          <thead className="border-b border-zinc-800 bg-zinc-900/80 text-xs uppercase text-zinc-500">
            <tr>
              <th className="px-4 py-3">#</th>
              <th className="px-4 py-3">Player</th>
              <th className="px-4 py-3">Country</th>
              <th className="px-4 py-3">Value</th>
              <th className="px-4 py-3">Updated</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => (
              <tr key={e.id} className="border-b border-zinc-800/80">
                <td className="px-4 py-3">{e.rank ?? "—"}</td>
                <td className="px-4 py-3">{e.playerName}</td>
                <td className="px-4 py-3">{e.countryName ?? "—"}</td>
                <td className="px-4 py-3">{e.value}</td>
                <td className="px-4 py-3">
                  {e.sourceUpdatedAt
                    ? new Date(e.sourceUpdatedAt).toLocaleString()
                    : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </ResponsiveDataTable>
      )}
    </div>
  );
}
