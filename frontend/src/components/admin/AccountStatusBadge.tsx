import { cn } from "@/lib/utils";

const styles: Record<string, string> = {
  Active: "bg-emerald-900/50 text-emerald-300",
  PendingVerification: "bg-amber-900/50 text-amber-300",
  Suspended: "bg-orange-900/50 text-orange-300",
  Banned: "bg-red-900/50 text-red-300",
};

const labels: Record<string, string> = {
  Active: "Active",
  PendingVerification: "Pending",
  Suspended: "Suspended",
  Banned: "Banned",
};

export function AccountStatusBadge({ status }: { status: string }) {
  return (
    <span
      className={cn(
        "inline-flex rounded-full px-2 py-0.5 text-xs font-medium",
        styles[status] ?? "bg-zinc-800 text-zinc-300"
      )}
    >
      {labels[status] ?? status}
    </span>
  );
}
