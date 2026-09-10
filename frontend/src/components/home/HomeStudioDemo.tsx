"use client";

import Link from "next/link";
import { ArrowRight, Clapperboard } from "lucide-react";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { ErrorState } from "@/components/ui/states";
import { FreshnessBadge } from "@/components/ui/freshness-badge";
import { buttonVariants } from "@/components/ui/button";
import { useStudioStories } from "@/hooks/useStudioStories";
import { stripHtml } from "@/lib/strip-html";
import type { StudioStoryCard } from "@/lib/types";
import { cn } from "@/lib/utils";

const STEPS = [
  { key: "take", label: "Take", hint: "Your call before kickoff" },
  { key: "receipt", label: "Receipt", hint: "What actually happened" },
  { key: "pack", label: "Pack", hint: "Studio export, sourced" },
] as const;

function pickStory(
  lists: Array<StudioStoryCard[] | undefined>
): StudioStoryCard | undefined {
  for (const list of lists) {
    const first = list?.[0];
    if (first) return first;
  }
  return undefined;
}

export function HomeStudioDemo() {
  const { data, isPending, isError, refetch, isFetching, isFetched } = useStudioStories();

  const takeStory = pickStory([data?.youVsPundits, data?.trending]);
  const receiptStory = pickStory([data?.latestReceipts, data?.youVsPundits]);
  const packStory = pickStory([data?.previousProjects, data?.latestReceipts, data?.trending]);
  const hasRealStories = Boolean(takeStory || receiptStory || packStory);

  const storiesByStep = [takeStory, receiptStory, packStory];
  const slots = STEPS.map((step, index) => ({
    ...step,
    title: stripHtml(storiesByStep[index]?.title ?? ""),
    summary: stripHtml(storiesByStep[index]?.summary ?? ""),
  }));

  return (
    <Panel
      title="From take to pack"
      subtitle={
        hasRealStories
          ? "Real Studio stories from this account"
          : "Product shape — labelled demo, not a generated pack"
      }
      accent="flare"
      action={
        <Link
          href="/studio"
          className={cn(
            buttonVariants({ size: "sm" }),
            "btn-tournament h-8 cursor-pointer px-3 text-[11px] font-bold uppercase tracking-wider"
          )}
        >
          <Clapperboard className="size-3" aria-hidden />
          Open Studio
        </Link>
      }
    >
      {isPending ? (
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-3">
          <Skeleton className="h-24 w-full rounded-lg" />
          <Skeleton className="h-24 w-full rounded-lg" />
          <Skeleton className="h-24 w-full rounded-lg" />
        </div>
      ) : isError ? (
        <ErrorState
          dense
          title="Studio stories unavailable"
          description="The take → receipt → pack path is still here. Open Studio when the request succeeds."
          onRetry={() => {
            void refetch();
          }}
        />
      ) : (
        <>
          {!hasRealStories ? (
            <p className="mb-2 inline-flex rounded-full border border-border bg-muted/40 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
              Demo shape
            </p>
          ) : isFetched && isFetching ? (
            <FreshnessBadge status="stale" label="Refreshing stories" className="mb-2" />
          ) : null}

          <ol className="grid grid-cols-1 gap-2 sm:grid-cols-3">
            {slots.map((slot, index) => (
              <li key={slot.key} className="relative min-w-0">
                <div className="rounded-lg border border-border bg-background/70 px-3 py-2.5">
                  <p className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                    {index + 1}. {slot.label}
                  </p>
                  {slot.title ? (
                    <>
                      <p className="mt-1 truncate text-sm font-semibold">{slot.title}</p>
                      {slot.summary ? (
                        <p className="mt-0.5 line-clamp-2 text-[11px] leading-snug text-muted-foreground">
                          {slot.summary}
                        </p>
                      ) : null}
                    </>
                  ) : (
                    <p className="mt-1 text-sm font-semibold">{slot.hint}</p>
                  )}
                </div>
                {index < slots.length - 1 ? (
                  <ArrowRight
                    className="pointer-events-none absolute top-1/2 -right-2 hidden size-3.5 -translate-y-1/2 text-muted-foreground sm:block"
                    aria-hidden
                  />
                ) : null}
              </li>
            ))}
          </ol>

          {!hasRealStories ? (
            <p className="mt-3 text-xs text-muted-foreground">
              Lock a pick, wait for full time, then open Studio. This diagram is the product shape — not a fake pack.
            </p>
          ) : null}
        </>
      )}
    </Panel>
  );
}
