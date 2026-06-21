"use client";

import { useState } from "react";
import { useAdminToast } from "@/components/admin/AdminToast";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminReview, useAdminReviewAction } from "@/hooks/admin/useAdmin";
import { getApiErrorMessage } from "@/lib/api";

export default function AdminReviewPage() {
  const { data: items, isLoading, refetch } = useAdminReview();
  const reviewAction = useAdminReviewAction();
  const { showToast } = useAdminToast();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editOpinion, setEditOpinion] = useState("");

  const startEdit = (id: string, opinion: string) => {
    setEditingId(id);
    setEditOpinion(opinion);
  };

  const approve = async (id: string) => {
    try {
      await reviewAction.mutateAsync({ id, action: "approve" });
      showToast("Approved");
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  const reject = async (id: string) => {
    try {
      await reviewAction.mutateAsync({ id, action: "reject", body: { notes: "Rejected by admin" } });
      showToast("Rejected");
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  const saveEdit = async (id: string) => {
    try {
      await reviewAction.mutateAsync({
        id,
        action: "update",
        body: { opinion: editOpinion },
      });
      showToast("Updated");
      setEditingId(null);
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold">Human Review Queue</h2>
        <p className="text-sm text-zinc-500">AI outputs flagged for manual review.</p>
      </div>

      {items?.length === 0 ? (
        <p className="text-zinc-500">Review queue is empty.</p>
      ) : (
        <div className="space-y-4">
          {items?.map((item) => (
            <div key={item.id} className="rounded-lg border border-zinc-800 p-4">
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <p className="text-sm font-medium">{item.punditName}</p>
                  <p className="text-xs text-zinc-500">
                    {item.sourceName} · confidence {item.confidence ?? "—"}
                  </p>
                </div>
                <div className="flex w-full flex-wrap gap-1 sm:w-auto">
                  <Button size="xs" onClick={() => approve(item.id)}>
                    Approve
                  </Button>
                  <Button size="xs" variant="outline" onClick={() => reject(item.id)}>
                    Reject
                  </Button>
                  <Button size="xs" variant="ghost" onClick={() => startEdit(item.id, item.opinion)}>
                    Edit
                  </Button>
                </div>
              </div>
              {editingId === item.id ? (
                <div className="mt-3 space-y-2">
                  <textarea
                    className="w-full rounded-md border border-zinc-700 bg-zinc-900 p-2 text-sm"
                    rows={4}
                    value={editOpinion}
                    onChange={(e) => setEditOpinion(e.target.value)}
                  />
                  <Button size="sm" onClick={() => saveEdit(item.id)}>
                    Save
                  </Button>
                </div>
              ) : (
                <p className="mt-3 text-sm">{item.opinion}</p>
              )}
              {item.prediction && (
                <p className="mt-2 text-sm text-zinc-400">Prediction: {item.prediction}</p>
              )}
              {item.evidenceQuote && (
                <blockquote className="mt-2 border-l-2 border-zinc-700 pl-3 text-xs text-zinc-500">
                  {item.evidenceQuote}
                </blockquote>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
