"use client";

import { useState } from "react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ResponsiveDataTable } from "@/components/admin/ResponsiveDataTable";
import {
  useAdminFootballCountries,
  useAdminFootballToggleActive,
} from "@/hooks/admin/useAdmin";
import { useAdminToast } from "@/components/admin/AdminToast";
import { getApiErrorMessage } from "@/lib/api";

export default function AdminFootballCountriesPage() {
  const [search, setSearch] = useState("");
  const { data, isLoading, refetch } = useAdminFootballCountries(search || undefined);
  const toggle = useAdminFootballToggleActive();
  const { showToast } = useAdminToast();

  const onToggle = async (id: string, isActive: boolean) => {
    try {
      await toggle.mutateAsync({ entity: "countries", id, isActive });
      showToast(isActive ? "Country enabled" : "Country disabled");
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  const countries = data?.countries ?? [];

  return (
    <div className="space-y-4">
      <Link href="/admin/football-data" className="text-sm text-sky-400 hover:underline">
        ← Football data
      </Link>
      <h2 className="text-xl font-semibold">Countries</h2>
      <input
        type="search"
        placeholder="Search…"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="h-9 w-full max-w-sm rounded border border-zinc-700 bg-zinc-900 px-3 text-sm"
      />
      {isLoading ? (
        <Skeleton className="h-48 w-full" />
      ) : (
        <ResponsiveDataTable>
          <thead className="border-b border-zinc-800 bg-zinc-900/80 text-xs uppercase text-zinc-500">
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Code</th>
              <th className="px-4 py-3">Provider</th>
              <th className="px-4 py-3">Active</th>
              <th className="px-4 py-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {countries.map((c) => (
              <tr key={c.id} className="border-b border-zinc-800/80">
                <td className="px-4 py-3">{c.name}</td>
                <td className="px-4 py-3">{c.code ?? "—"}</td>
                <td className="px-4 py-3">{c.externalProvider ?? "—"}</td>
                <td className="px-4 py-3">{c.isActive ? "Yes" : "No"}</td>
                <td className="px-4 py-3">
                  <Button
                    size="xs"
                    variant="outline"
                    onClick={() => onToggle(c.id, !c.isActive)}
                  >
                    {c.isActive ? "Disable" : "Enable"}
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
