"use client";

import { useMemo, useState } from "react";
import {
  AdminMobileCard,
  AdminMobileCardRow,
  ResponsiveDataTable,
} from "@/components/admin/ResponsiveDataTable";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminAuditLogs } from "@/hooks/admin/useAdmin";

const PAGE_SIZE = 50;

export default function AdminAuditPage() {
  const [action, setAction] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);

  const { data, isLoading, isError, isFetching, refetch } = useAdminAuditLogs({
    page,
    pageSize: PAGE_SIZE,
    action: action || undefined,
    from: from ? new Date(from).toISOString() : undefined,
    to: to ? new Date(`${to}T23:59:59`).toISOString() : undefined,
  });

  const totalPages = useMemo(
    () => (data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1),
    [data]
  );

  const resetFilters = () => {
    setAction("");
    setFrom("");
    setTo("");
    setPage(1);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold">Audit Log</h2>
          <p className="text-sm text-zinc-500">
            Every privileged admin action. Read-only; entries cannot be edited or deleted here.
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => refetch()} disabled={isFetching}>
          Refresh
        </Button>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <select
          className="w-full rounded-md border border-zinc-700 bg-zinc-900 px-3 py-2.5 text-sm"
          aria-label="Filter by action"
          value={action}
          onChange={(e) => {
            setAction(e.target.value);
            setPage(1);
          }}
        >
          <option value="">All actions</option>
          {data?.availableActions.map((a) => (
            <option key={a} value={a}>
              {a}
            </option>
          ))}
        </select>
        <Input
          type="date"
          aria-label="From date"
          value={from}
          onChange={(e) => {
            setFrom(e.target.value);
            setPage(1);
          }}
        />
        <Input
          type="date"
          aria-label="To date"
          value={to}
          onChange={(e) => {
            setTo(e.target.value);
            setPage(1);
          }}
        />
        <Button variant="outline" onClick={resetFilters}>
          Clear filters
        </Button>
      </div>

      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : isError ? (
        <p className="text-destructive">Failed to load the audit log.</p>
      ) : data && data.items.length === 0 ? (
        <p className="text-zinc-500">No audit entries match these filters.</p>
      ) : (
        <>
          <ResponsiveDataTable
            minWidth="900px"
            mobileCards={data?.items.map((entry) => (
              <AdminMobileCard key={entry.id}>
                <p className="font-mono text-sm font-medium">{entry.action}</p>
                <AdminMobileCardRow label="When">
                  {new Date(entry.createdAt).toLocaleString()}
                </AdminMobileCardRow>
                <AdminMobileCardRow label="Target">
                  {entry.targetType}
                  {entry.targetId ? ` · ${entry.targetId}` : ""}
                </AdminMobileCardRow>
                <AdminMobileCardRow label="Admin">
                  <span className="font-mono text-xs">{entry.adminUserId}</span>
                </AdminMobileCardRow>
                {entry.metadataJson ? (
                  <AdminMobileCardRow label="Metadata">
                    <span className="font-mono text-xs">{entry.metadataJson}</span>
                  </AdminMobileCardRow>
                ) : null}
              </AdminMobileCard>
            ))}
          >
            <thead className="bg-zinc-900/60 text-xs uppercase tracking-wide text-zinc-500">
              <tr>
                <th className="px-4 py-3 font-medium">When</th>
                <th className="px-4 py-3 font-medium">Action</th>
                <th className="px-4 py-3 font-medium">Target</th>
                <th className="px-4 py-3 font-medium">Admin</th>
                <th className="px-4 py-3 font-medium">Metadata</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((entry) => (
                <tr key={entry.id} className="border-t border-zinc-800 align-top">
                  <td className="whitespace-nowrap px-4 py-3 text-zinc-400">
                    {new Date(entry.createdAt).toLocaleString()}
                  </td>
                  <td className="px-4 py-3 font-mono text-xs text-zinc-200">{entry.action}</td>
                  <td className="px-4 py-3">
                    <span className="text-zinc-300">{entry.targetType}</span>
                    {entry.targetId ? (
                      <p className="break-anywhere font-mono text-xs text-zinc-600">
                        {entry.targetId}
                      </p>
                    ) : null}
                  </td>
                  <td className="break-anywhere px-4 py-3 font-mono text-xs text-zinc-500">
                    {entry.adminUserId}
                  </td>
                  <td className="break-anywhere px-4 py-3 font-mono text-xs text-zinc-500">
                    {entry.metadataJson ?? "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </ResponsiveDataTable>

          <div className="flex items-center justify-between gap-4 text-sm text-zinc-500">
            <span>
              Page {data?.page ?? 1} of {totalPages} · {data?.total ?? 0} entries
            </span>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="outline"
                disabled={page <= 1 || isFetching}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                Previous
              </Button>
              <Button
                size="sm"
                variant="outline"
                disabled={page >= totalPages || isFetching}
                onClick={() => setPage((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
