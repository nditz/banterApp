"use client";

import { useAdminToast } from "@/components/admin/AdminToast";
import {
  AdminMobileCard,
  AdminMobileCardRow,
  ResponsiveDataTable,
} from "@/components/admin/ResponsiveDataTable";
import { StatusBadge } from "@/components/admin/StatusBadge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminSourceAction, useAdminSources } from "@/hooks/admin/useAdmin";
import { getApiErrorMessage } from "@/lib/api";
import type { AdminSource } from "@/lib/admin/types";

function SourceActions({
  source,
  onAct,
}: {
  source: AdminSource;
  onAct: (id: string, action: "sync" | "enable" | "disable") => void;
}) {
  return (
    <div className="flex flex-wrap gap-1">
      <Button size="xs" onClick={() => onAct(source.sourceId, "sync")}>
        Sync
      </Button>
      {source.enabled ? (
        <Button size="xs" variant="outline" onClick={() => onAct(source.sourceId, "disable")}>
          Disable
        </Button>
      ) : (
        <Button size="xs" variant="outline" onClick={() => onAct(source.sourceId, "enable")}>
          Enable
        </Button>
      )}
    </div>
  );
}

export default function AdminSourcesPage() {
  const { data: sources, isLoading, refetch } = useAdminSources();
  const sourceAction = useAdminSourceAction();
  const { showToast } = useAdminToast();

  const act = async (id: string, action: "sync" | "enable" | "disable") => {
    try {
      await sourceAction.mutateAsync({ id, action });
      showToast(`Source ${action}`);
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold sm:text-2xl">Sources</h2>
        <p className="text-sm text-zinc-500">
          RSS URLs live in the database and are refreshed by the RSS Feed Resolve job.
          Higher-priority channels are fetched first.
        </p>
      </div>

      <ResponsiveDataTable
        mobileCards={sources?.map((s) => (
          <AdminMobileCard key={s.sourceId}>
            <div className="mb-2 flex flex-wrap items-start justify-between gap-2">
              <p className="font-medium">{s.name}</p>
              <StatusBadge status={s.enabled ? "idle" : "disabled"} />
            </div>
            <p className="mb-3 break-anywhere text-xs text-zinc-500">{s.url}</p>
            <AdminMobileCardRow label="Type">
              {s.type}
              {s.lastHttpStatus != null ? ` · HTTP ${s.lastHttpStatus}` : ""}
            </AdminMobileCardRow>
            <AdminMobileCardRow label="Items / Failures">
              {s.itemsIngested} / {s.failureCount}
            </AdminMobileCardRow>
            <div className="mt-3">
              <SourceActions source={s} onAct={act} />
            </div>
          </AdminMobileCard>
        ))}
      >
        <thead className="border-b border-zinc-800 bg-zinc-900/80 text-xs uppercase text-zinc-500">
          <tr>
            <th className="px-4 py-3">Name</th>
            <th className="hidden px-4 py-3 sm:table-cell">Type</th>
            <th className="px-4 py-3">Status</th>
            <th className="hidden px-4 py-3 md:table-cell">Items</th>
            <th className="hidden px-4 py-3 lg:table-cell">Failures</th>
            <th className="px-4 py-3">Actions</th>
          </tr>
        </thead>
        <tbody>
          {sources?.map((s) => (
            <tr key={s.sourceId} className="border-b border-zinc-800/80">
              <td className="px-4 py-3">
                <p className="font-medium">{s.name}</p>
                <p className="max-w-xs break-anywhere text-xs text-zinc-500">{s.url}</p>
              </td>
              <td className="hidden px-4 py-3 sm:table-cell">
                {s.type}
                {s.lastHttpStatus != null ? ` · ${s.lastHttpStatus}` : ""}
              </td>
              <td className="px-4 py-3">
                <StatusBadge status={s.enabled ? "idle" : "disabled"} />
              </td>
              <td className="hidden px-4 py-3 tabular-nums md:table-cell">{s.itemsIngested}</td>
              <td className="hidden px-4 py-3 tabular-nums lg:table-cell">{s.failureCount}</td>
              <td className="px-4 py-3">
                <SourceActions source={s} onAct={act} />
              </td>
            </tr>
          ))}
        </tbody>
      </ResponsiveDataTable>
    </div>
  );
}
