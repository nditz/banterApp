"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  AdminMobileCard,
  AdminMobileCardRow,
  ResponsiveDataTable,
} from "@/components/admin/ResponsiveDataTable";
import { StatCard } from "@/components/admin/StatCard";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminUsers } from "@/hooks/admin/useAdmin";
import { AccountStatusBadge } from "@/components/admin/AccountStatusBadge";
import { cn } from "@/lib/utils";

const PAGE_SIZE = 25;

export default function AdminUsersPage() {
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  // Debounce so typing does not fire a request per keystroke.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const { data, isLoading, isError, refetch, isFetching } = useAdminUsers({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
  });

  const totalPages = useMemo(
    () => (data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1),
    [data]
  );

  const adminCount = data?.items.filter((u) => u.isPlatformAdmin).length ?? 0;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold">Users</h2>
          <p className="text-sm text-zinc-500">
            Accounts that have signed in through Supabase. Guest sessions are not listed here.
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => refetch()} disabled={isFetching}>
          Refresh
        </Button>
      </div>

      {data?.warning ? (
        <div className="rounded-lg border border-amber-900/60 bg-amber-950/30 p-4 text-sm text-amber-200">
          {data.warning}
        </div>
      ) : null}

      <div className="grid gap-3 sm:grid-cols-3">
        <StatCard label="Total accounts" value={data?.total ?? "—"} />
        <StatCard label="Admins on this page" value={adminCount} />
        <StatCard
          label="Identity source"
          value={data?.identitySource === "supabase" ? "Supabase" : "Database"}
          sub={
            data?.identitySource === "supabase"
              ? "Sign-in history available on user detail"
              : "Service-role key not configured"
          }
        />
      </div>

      <Input
        className="w-full max-w-md"
        placeholder="Search by email or display name"
        type="search"
        value={searchInput}
        onChange={(e) => setSearchInput(e.target.value)}
      />

      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : isError ? (
        <p className="text-destructive">Failed to load users.</p>
      ) : data && data.items.length === 0 ? (
        <p className="text-zinc-500">
          {search ? `No users match "${search}".` : "No registered accounts yet."}
        </p>
      ) : (
        <>
          <ResponsiveDataTable
            minWidth="880px"
            mobileCards={data?.items.map((user) => (
              <AdminMobileCard key={user.id}>
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="truncate font-medium">{user.displayName || "—"}</p>
                    <p className="break-anywhere text-xs text-zinc-500">{user.email}</p>
                  </div>
                  <AccountStatusBadge status={user.accountStatus} />
                </div>
                <AdminMobileCardRow label="Admin">
                  {user.isPlatformAdmin ? <Badge>Admin</Badge> : <span className="text-zinc-500">No</span>}
                </AdminMobileCardRow>
                <AdminMobileCardRow label="Predictions">{user.predictionCount}</AdminMobileCardRow>
                <AdminMobileCardRow label="Leagues">{user.leagueCount}</AdminMobileCardRow>
                <AdminMobileCardRow label="Joined">
                  {new Date(user.createdAt).toLocaleDateString()}
                </AdminMobileCardRow>
                <div className="mt-3">
                  <Link
                    href={`/admin/users/${user.id}`}
                    className={cn(buttonVariants({ size: "xs", variant: "outline" }))}
                  >
                    View
                  </Link>
                </div>
              </AdminMobileCard>
            ))}
          >
            <thead className="bg-zinc-900/60 text-xs uppercase tracking-wide text-zinc-500">
              <tr>
                <th className="px-4 py-3 font-medium">User</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Role</th>
                <th className="px-4 py-3 text-right font-medium">Predictions</th>
                <th className="px-4 py-3 text-right font-medium">Leagues</th>
                <th className="px-4 py-3 font-medium">Joined</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {data?.items.map((user) => (
                <tr key={user.id} className="border-t border-zinc-800">
                  <td className="px-4 py-3">
                    <p className="font-medium">{user.displayName || "—"}</p>
                    <p className="break-anywhere text-xs text-zinc-500">{user.email}</p>
                  </td>
                  <td className="px-4 py-3">
                    <AccountStatusBadge status={user.accountStatus} />
                  </td>
                  <td className="px-4 py-3">
                    {user.isPlatformAdmin ? (
                      <Badge>Admin</Badge>
                    ) : (
                      <span className="text-zinc-500">Member</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right tabular-nums">{user.predictionCount}</td>
                  <td className="px-4 py-3 text-right tabular-nums">{user.leagueCount}</td>
                  <td className="px-4 py-3 text-zinc-400">
                    {new Date(user.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link
                      href={`/admin/users/${user.id}`}
                      className={cn(buttonVariants({ size: "xs", variant: "outline" }))}
                    >
                      View
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </ResponsiveDataTable>

          <div className="flex items-center justify-between gap-4 text-sm text-zinc-500">
            <span>
              Page {data?.page ?? 1} of {totalPages} · {data?.total ?? 0} accounts
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
