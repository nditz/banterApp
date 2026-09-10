"use client";

import Link from "next/link";
import { Mic2 } from "lucide-react";
import { Button, buttonVariants } from "@/components/ui/button";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { FreshnessBadge } from "@/components/ui/freshness-badge";
import { useTermsSaveGate } from "@/components/session/TermsSaveGate";
import { useFollowPundit, usePunditDirectory } from "@/hooks/usePundits";
import { getApiErrorMessage } from "@/lib/api";
import {
  formatPunditSubtitle,
  formatSourcePlatformLabel,
  getPunditAvatarUrl,
} from "@/lib/pundits";
import type { PunditDirectoryEntry } from "@/lib/types";
import { cn } from "@/lib/utils";

const STRIP_SIZE = 6;

export function HomePunditStrip() {
  const { data, isPending, isError, refetch, isFetching, isFetched } = usePunditDirectory();
  const { follow, unfollow } = useFollowPundit();
  const { requireTerms } = useTermsSaveGate();

  const pundits = [...(data ?? [])]
    .sort((a, b) => Number(a.isFollowed) - Number(b.isFollowed))
    .slice(0, STRIP_SIZE);

  const pendingId = follow.isPending
    ? (follow.variables as string | undefined)
    : unfollow.isPending
      ? (unfollow.variables as string | undefined)
      : undefined;
  const actionError = follow.error ?? unfollow.error;

  return (
    <Panel
      title="Desks to follow"
      subtitle="Sourced voices — we never invent quotes"
      accent="gold"
      action={
        <Link
          href="/pundits"
          className={cn(buttonVariants({ variant: "ghost", size: "sm" }), "text-[11px] font-semibold")}
        >
          All pundits
        </Link>
      }
    >
      {actionError ? (
        <p role="alert" className="mb-2 text-xs text-muted-foreground">
          {getApiErrorMessage(actionError)}
        </p>
      ) : null}

      {isPending ? (
        <ul className="flex gap-2 overflow-x-auto pb-1 scrollbar-none sm:grid sm:grid-cols-3 sm:overflow-visible lg:grid-cols-6">
          {Array.from({ length: 4 }, (_, i) => (
            <li key={i} className="min-w-[11rem] sm:min-w-0">
              <Skeleton className="h-[7.5rem] w-full rounded-lg" />
            </li>
          ))}
        </ul>
      ) : isError ? (
        <ErrorState
          dense
          title="Pundits unavailable"
          description="The sourced directory could not be loaded."
          onRetry={() => {
            void refetch();
          }}
        />
      ) : pundits.length === 0 ? (
        <EmptyState
          dense
          icon={Mic2}
          title="No sourced desks yet"
          description="Followable pundits appear here after extraction and review — never as invented quotes."
          action={
            <Link href="/pundits" className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
              Open pundits
            </Link>
          }
        />
      ) : (
        <>
          {isFetched && isFetching ? (
            <FreshnessBadge status="stale" label="Refreshing sourced desks" className="mb-2" />
          ) : null}
          <ul className="flex gap-2 overflow-x-auto pb-1 scrollbar-none sm:grid sm:grid-cols-3 sm:overflow-visible lg:grid-cols-6">
            {pundits.map((pundit) => (
              <HomePunditCard
                key={pundit.id}
                pundit={pundit}
                busy={pendingId === pundit.id}
                onFollow={async () => {
                  if (!(await requireTerms())) return;
                  follow.mutate(pundit.id);
                }}
                onUnfollow={async () => {
                  if (!(await requireTerms())) return;
                  unfollow.mutate(pundit.id);
                }}
              />
            ))}
          </ul>
        </>
      )}
    </Panel>
  );
}

function HomePunditCard({
  pundit,
  busy,
  onFollow,
  onUnfollow,
}: {
  pundit: PunditDirectoryEntry;
  busy: boolean;
  onFollow: () => void | Promise<void>;
  onUnfollow: () => void | Promise<void>;
}) {
  const subtitle = formatPunditSubtitle({
    parodyCue: pundit.parodyCue ?? undefined,
    archetype: pundit.archetype ?? undefined,
    organization: pundit.organization ?? undefined,
  });
  const platform = formatSourcePlatformLabel(pundit.sourcePlatform ?? undefined);

  return (
    <li className="min-w-[11.5rem] rounded-lg border border-border bg-background/70 p-3 sm:min-w-0">
      <div className="flex items-start gap-2">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={getPunditAvatarUrl(pundit.avatarSeed ?? undefined, pundit.name)}
          alt=""
          width={32}
          height={32}
          className="size-8 shrink-0 rounded-md bg-muted"
        />
        <div className="min-w-0">
          <p className="truncate text-sm font-semibold">{pundit.name}</p>
          {subtitle ? (
            <p className="truncate text-[11px] text-muted-foreground">{subtitle}</p>
          ) : null}
        </div>
      </div>
      <p className="mt-2 truncate text-[11px] text-muted-foreground">
        {pundit.predictionCount > 0
          ? `${pundit.predictionCount} match pick${pundit.predictionCount === 1 ? "" : "s"}`
          : "Sourced desk"}
        {platform ? ` · ${platform}` : ""}
      </p>
      {pundit.sourceUrl ? (
        <a
          href={pundit.sourceUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="mt-0.5 inline-block text-[11px] text-pitch underline-offset-2 hover:underline"
        >
          Source
        </a>
      ) : null}
      <Button
        type="button"
        size="sm"
        variant={pundit.isFollowed ? "outline" : "default"}
        disabled={busy}
        aria-pressed={pundit.isFollowed}
        className="mt-2 w-full"
        onClick={() => (pundit.isFollowed ? onUnfollow() : onFollow())}
      >
        {pundit.isFollowed ? "Following" : "Follow"}
      </Button>
    </li>
  );
}
