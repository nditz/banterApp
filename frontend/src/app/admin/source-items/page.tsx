"use client";

import { useState } from "react";
import { StatusBadge } from "@/components/admin/StatusBadge";
import { useAdminToast } from "@/components/admin/AdminToast";
import {
  AdminMobileCard,
  AdminMobileCardRow,
  ResponsiveDataTable,
} from "@/components/admin/ResponsiveDataTable";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminReprocessItem, useAdminSourceItems } from "@/hooks/admin/useAdmin";
import { getApiErrorMessage } from "@/lib/api";

export default function AdminSourceItemsPage() {
  const [needsReview, setNeedsReview] = useState(false);
  const { data: items, isLoading, refetch } = useAdminSourceItems(
    needsReview ? { needsReview: true } : undefined
  );
  const reprocess = useAdminReprocessItem();
  const { showToast } = useAdminToast();

  const handleReprocess = async (id: string) => {
    try {
      await reprocess.mutateAsync(id);
      showToast("Reprocess queued");
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold sm:text-2xl">Source Items</h2>
          <p className="text-sm text-zinc-500">Ingested media and articles.</p>
        </div>
        <label className="flex min-h-11 items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={needsReview}
            onChange={(e) => setNeedsReview(e.target.checked)}
          />
          Needs review only
        </label>
      </div>

      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : (
        <ResponsiveDataTable
          mobileCards={items?.map((item) => (
            <AdminMobileCard key={item.id}>
              <p className="mb-1 font-medium break-anywhere">{item.title}</p>
              {item.processingError && (
                <p className="mb-2 text-xs text-red-400 break-anywhere">{item.processingError}</p>
              )}
              <AdminMobileCardRow label="Source">
                {item.sourceName} ({item.sourceType})
              </AdminMobileCardRow>
              <AdminMobileCardRow label="Status">
                <StatusBadge status={item.status === "failed" ? "failed" : "idle"} />
              </AdminMobileCardRow>
              <AdminMobileCardRow label="Flags">
                {[item.hasRawText && "text", item.hasPredictions && "predictions", item.needsHumanReview && "review"]
                  .filter(Boolean)
                  .join(", ") || "—"}
              </AdminMobileCardRow>
              <Button
                size="sm"
                variant="outline"
                className="mt-3 w-full sm:w-auto"
                onClick={() => handleReprocess(item.id)}
              >
                Reprocess
              </Button>
            </AdminMobileCard>
          ))}
        >
          <thead className="border-b border-zinc-800 bg-zinc-900/80 text-xs uppercase text-zinc-500">
            <tr>
              <th className="px-4 py-3">Title</th>
              <th className="hidden px-4 py-3 sm:table-cell">Source</th>
              <th className="px-4 py-3">Status</th>
              <th className="hidden px-4 py-3 md:table-cell">Flags</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {items?.map((item) => (
              <tr key={item.id} className="border-b border-zinc-800/80">
                <td className="max-w-xs px-4 py-3">
                  <p className="truncate font-medium">{item.title}</p>
                  {item.processingError && (
                    <p className="truncate text-xs text-red-400">{item.processingError}</p>
                  )}
                </td>
                <td className="hidden px-4 py-3 text-xs sm:table-cell">
                  {item.sourceName}
                  <br />
                  <span className="text-zinc-500">{item.sourceType}</span>
                </td>
                <td className="px-4 py-3">
                  <StatusBadge status={item.status === "failed" ? "failed" : "idle"} />
                </td>
                <td className="hidden px-4 py-3 text-xs text-zinc-400 md:table-cell">
                  {item.hasRawText ? "text " : ""}
                  {item.hasPredictions ? "predictions " : ""}
                  {item.needsHumanReview ? "review" : ""}
                </td>
                <td className="px-4 py-3">
                  <Button size="xs" variant="outline" onClick={() => handleReprocess(item.id)}>
                    Reprocess
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
