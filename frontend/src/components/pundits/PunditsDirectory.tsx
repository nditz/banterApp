"use client";

import Link from "next/link";
import { Mic2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/states";
import { getPunditAvatarUrl, formatPunditSubtitle, formatSourcePlatformLabel } from "@/lib/pundits";
import { useFollowPundit, usePunditDirectory } from "@/hooks/usePundits";
import type { PunditDirectoryEntry } from "@/lib/types";
import { getApiErrorMessage } from "@/lib/api";

export function PunditsDirectory() {
  const { data, isPending, isError, error } = usePunditDirectory();
  const { follow, unfollow } = useFollowPundit();
  const pundits = data ?? [];
  const followedCount = pundits.filter((p) => p.isFollowed).length;
  const pendingId = follow.isPending
    ? (follow.variables as string | undefined)
    : unfollow.isPending
      ? (unfollow.variables as string | undefined)
      : undefined;
  const actionError = follow.error ?? unfollow.error;

  return (
    <div className="mx-auto max-w-4xl space-y-5">
      <header>
        <p className="page-kicker">Compare your takes</p>
        <h1 className="mt-3 text-2xl font-bold sm:text-3xl">Pundits</h1>
        <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
          Follow sourced voices. Their match-linked picks show up on Matchweek, in Studio, and in your
          feed — with the original source attached. We never invent quotes.
        </p>
        <p className="mt-2 text-xs text-muted-foreground">
          Following {followedCount} ·{" "}
          <Link href="/studio" className="font-semibold text-foreground hover:underline">
            Open Studio
          </Link>
        </p>
      </header>

      {actionError ? (
        <p role="alert" className="text-sm text-muted-foreground">
          {getApiErrorMessage(actionError)}
        </p>
      ) : null}

      {isPending ? (
        <p className="text-sm text-muted-foreground">Loading sourced pundits…</p>
      ) : isError ? (
        <p role="alert" className="text-sm text-muted-foreground">
          {getApiErrorMessage(error)} Pundit list could not be loaded.
        </p>
      ) : pundits.length === 0 ? (
        <EmptyState
          dense
          icon={Mic2}
          title="No sourced pundits yet"
          description="Extraction jobs and admin review fill this list. An empty list can mean ingest is failing."
        />
      ) : (
        <ul className="space-y-3">
          {pundits.map((pundit) => (
            <PunditFollowCard
              key={pundit.id}
              pundit={pundit}
              busy={pendingId === pundit.id}
              onFollow={() => follow.mutate(pundit.id)}
              onUnfollow={() => unfollow.mutate(pundit.id)}
            />
          ))}
        </ul>
      )}
    </div>
  );
}

function PunditFollowCard({
  pundit,
  busy,
  onFollow,
  onUnfollow,
}: {
  pundit: PunditDirectoryEntry;
  busy: boolean;
  onFollow: () => void;
  onUnfollow: () => void;
}) {
  const subtitle = formatPunditSubtitle({
    parodyCue: pundit.parodyCue ?? undefined,
    archetype: pundit.archetype ?? undefined,
    organization: pundit.organization ?? undefined,
  });

  return (
    <li className="flex flex-wrap items-start gap-3 rounded-md border border-border bg-card px-4 py-3 shadow-sm">
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        src={getPunditAvatarUrl(pundit.avatarSeed ?? undefined, pundit.name)}
        alt=""
        className="size-10 shrink-0 rounded-md bg-muted"
      />
      <div className="min-w-0 flex-1">
        <p className="flex items-center gap-1.5 text-sm font-semibold">
          <Mic2 className="size-3.5 text-muted-foreground" aria-hidden />
          {pundit.name}
        </p>
        {subtitle ? <p className="text-xs text-muted-foreground">{subtitle}</p> : null}
        {pundit.attributionNote ? (
          <p className="mt-1 text-[11px] leading-snug text-muted-foreground">{pundit.attributionNote}</p>
        ) : null}
        <p className="mt-1 text-[11px] text-muted-foreground">
          {pundit.predictionCount} match pick{pundit.predictionCount === 1 ? "" : "s"}
          {pundit.sourcePlatform
            ? ` · via ${formatSourcePlatformLabel(pundit.sourcePlatform)}`
            : ""}
          {pundit.sourceUrl ? (
            <>
              {" · "}
              <a
                href={pundit.sourceUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="text-pitch underline-offset-2 hover:underline"
              >
                Source
              </a>
            </>
          ) : null}
        </p>
      </div>
      <Button
        type="button"
        size="sm"
        variant={pundit.isFollowed ? "outline" : "default"}
        disabled={busy}
        aria-pressed={pundit.isFollowed}
        onClick={() => (pundit.isFollowed ? onUnfollow() : onFollow())}
      >
        {pundit.isFollowed ? "Following" : "Follow"}
      </Button>
    </li>
  );
}
