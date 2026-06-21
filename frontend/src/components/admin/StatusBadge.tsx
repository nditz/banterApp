import { cn } from "@/lib/utils";

const styles: Record<string, string> = {
  idle: "bg-zinc-800 text-zinc-300",
  running: "bg-blue-900/50 text-blue-300",
  paused: "bg-amber-900/50 text-amber-300",
  failed: "bg-red-900/50 text-red-300",
  disabled: "bg-zinc-900 text-zinc-500",
  success: "bg-emerald-900/50 text-emerald-300",
  open: "bg-red-900/50 text-red-300",
  resolved: "bg-emerald-900/50 text-emerald-300",
  ignored: "bg-zinc-800 text-zinc-400",
};

export function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={cn(
        "inline-flex rounded-full px-2 py-0.5 text-xs font-medium capitalize",
        styles[status] ?? "bg-zinc-800 text-zinc-300"
      )}
    >
      {status}
    </span>
  );
}
