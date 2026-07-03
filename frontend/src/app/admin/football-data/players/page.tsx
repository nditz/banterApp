"use client";

import { useState } from "react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ResponsiveDataTable } from "@/components/admin/ResponsiveDataTable";
import {
  useAdminFootballPlayers,
  useAdminFootballToggleActive,
} from "@/hooks/admin/useAdmin";
import { useAdminToast } from "@/components/admin/AdminToast";
import { getApiErrorMessage } from "@/lib/api";

export default function AdminFootballPlayersPage() {
  const [search, setSearch] = useState("");
  const [position, setPosition] = useState("");
  const { data, isLoading, refetch } = useAdminFootballPlayers({
    search: search || undefined,
    position: position || undefined,
  });
  const toggle = useAdminFootballToggleActive();
  const { showToast } = useAdminToast();

  const onToggle = async (id: string, isActive: boolean) => {
    try {
      await toggle.mutateAsync({ entity: "players", id, isActive });
      showToast(isActive ? "Player enabled" : "Player disabled");
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  const players = data?.players ?? [];

  return (
    <div className="space-y-4">
      <Link href="/admin/football-data" className="text-sm text-sky-400 hover:underline">
        ← Football data
      </Link>
      <h2 className="text-xl font-semibold">Players</h2>
      <div className="flex flex-wrap gap-2">
        <input
          type="search"
          placeholder="Search players…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="h-9 rounded border border-zinc-700 bg-zinc-900 px-3 text-sm"
        />
        <input
          type="text"
          placeholder="Position filter"
          value={position}
          onChange={(e) => setPosition(e.target.value)}
          className="h-9 rounded border border-zinc-700 bg-zinc-900 px-3 text-sm"
        />
      </div>
      {isLoading ? (
        <Skeleton className="h-48 w-full" />
      ) : (
        <ResponsiveDataTable>
          <thead className="border-b border-zinc-800 bg-zinc-900/80 text-xs uppercase text-zinc-500">
            <tr>
              <th className="px-4 py-3">Player</th>
              <th className="px-4 py-3">Country</th>
              <th className="px-4 py-3">Pos</th>
              <th className="px-4 py-3">Active</th>
              <th className="px-4 py-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {players.map((p) => (
              <tr key={p.id} className="border-b border-zinc-800/80">
                <td className="px-4 py-3">{p.displayName}</td>
                <td className="px-4 py-3">{p.countryName ?? "—"}</td>
                <td className="px-4 py-3">{p.position ?? "—"}</td>
                <td className="px-4 py-3">{p.isActive ? "Yes" : "No"}</td>
                <td className="px-4 py-3">
                  <Button
                    size="xs"
                    variant="outline"
                    onClick={() => onToggle(p.id, !p.isActive)}
                  >
                    {p.isActive ? "Disable" : "Enable"}
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </ResponsiveDataTable>
      )}
    </div>
  );
}
