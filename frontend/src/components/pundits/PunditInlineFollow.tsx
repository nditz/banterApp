"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import { useTermsSaveGate } from "@/components/session/TermsSaveGate";
import { useFollowPundit, usePunditDirectory } from "@/hooks/usePundits";
import type { PunditDirectoryEntry } from "@/lib/types";

function findPunditByName(
  directory: PunditDirectoryEntry[] | undefined,
  name: string
): PunditDirectoryEntry | undefined {
  const needle = name.trim().toLowerCase();
  if (!needle) return undefined;
  return directory?.find((pundit) => pundit.name.trim().toLowerCase() === needle);
}

export function PunditInlineFollow({ name }: { name: string }) {
  const { data: directory } = usePunditDirectory();
  const { follow, unfollow } = useFollowPundit();
  const { requireTerms } = useTermsSaveGate();
  const entry = findPunditByName(directory, name);
  const pendingId = follow.isPending
    ? (follow.variables as string | undefined)
    : unfollow.isPending
      ? (unfollow.variables as string | undefined)
      : undefined;
  const busy = Boolean(entry && pendingId === entry.id);

  if (!entry) {
    return (
      <Link
        href="/pundits"
        className="text-[10px] font-semibold text-foreground hover:underline"
      >
        Follow
      </Link>
    );
  }

  return (
    <Button
      type="button"
      size="xs"
      variant={entry.isFollowed ? "outline" : "default"}
      disabled={busy}
      aria-pressed={entry.isFollowed}
      className="h-5 px-1.5 text-[10px]"
      onClick={async () => {
        if (!(await requireTerms())) return;
        if (entry.isFollowed) {
          unfollow.mutate(entry.id);
        } else {
          follow.mutate(entry.id);
        }
      }}
    >
      {entry.isFollowed ? "Following" : "Follow"}
    </Button>
  );
}
