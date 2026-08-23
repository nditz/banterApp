"use client";

import { useCallback, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { MatchCard } from "@/components/prediction/MatchCard";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { useCurrentMatchweek, useMatches } from "@/hooks/useMatches";
import { isMatchLocked } from "@/lib/anonymous";
import type { Match } from "@/lib/types";
import { cn } from "@/lib/utils";

const MATCHES_PER_PAGE = 3;
const SWIPE_THRESHOLD_PX = 40;
const MAX_DOT_PAGES = 8;

function chunk<T>(items: T[], size: number): T[][] {
  const pages: T[][] = [];
  for (let i = 0; i < items.length; i += size) {
    pages.push(items.slice(i, i + size));
  }
  return pages;
}

export function PredictionCenter() {
  const { data: currentWeek, isLoading: weekLoading, isError: weekError } = useCurrentMatchweek();
  const { data: upcoming, isLoading: upcomingLoading, isError: upcomingError } = useMatches();
  const [page, setPage] = useState(0);
  const touchStartX = useRef<number | null>(null);

  const weekMatches = currentWeek?.matches;
  const isLoading = weekLoading || ((weekMatches?.length ?? 0) === 0 && upcomingLoading);
  const isError = weekError || upcomingError;

  const openMatches = useMemo(() => {
    const matches = weekMatches && weekMatches.length > 0 ? weekMatches : upcoming ?? [];
    return matches.filter((match) => !isMatchLocked(match));
  }, [weekMatches, upcoming]);

  const pages = useMemo(
    () => chunk<Match>(openMatches, MATCHES_PER_PAGE),
    [openMatches]
  );
  const pageCount = pages.length;
  const safePage = Math.min(page, Math.max(0, pageCount - 1));

  const goTo = useCallback(
    (next: number) => {
      if (pageCount === 0) return;
      setPage(Math.max(0, Math.min(next, pageCount - 1)));
    },
    [pageCount]
  );

  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartX.current = e.touches[0].clientX;
  };

  const handleTouchEnd = (e: React.TouchEvent) => {
    if (touchStartX.current === null) return;
    const delta = e.changedTouches[0].clientX - touchStartX.current;
    touchStartX.current = null;
    if (Math.abs(delta) < SWIPE_THRESHOLD_PX) return;
    goTo(delta < 0 ? safePage + 1 : safePage - 1);
  };

  return (
    <Panel
      id="prediction-center-heading"
      title={currentWeek?.number ? `Matchweek ${currentWeek.number}` : "Matchweek picks"}
      subtitle="Premier League fixtures — lock in before kickoff"
      accent="pitch"
      className="xl:flex xl:max-h-[calc(100vh-6.5rem)] xl:min-h-[34rem] xl:flex-col"
      bodyClassName="xl:flex xl:min-h-0 xl:flex-1 xl:flex-col"
    >
      {isError && (
        <p className="mb-3 shrink-0 text-xs text-muted-foreground">
          Demo fixtures shown
        </p>
      )}

      {isLoading ? (
        <div className="space-y-3">
          {Array.from({ length: MATCHES_PER_PAGE }).map((_, i) => (
            <Skeleton key={i} className="h-40 w-full rounded-lg" />
          ))}
        </div>
      ) : pageCount === 0 ? (
        <p className="py-8 text-center text-sm text-muted-foreground">
          No open fixtures this matchweek. Check the full board for upcoming weeks.
        </p>
      ) : (
        <>
          {/* Sliding viewport: pages sit side by side and the track translates */}
          <div
            className="overflow-hidden xl:min-h-0 xl:flex-1 xl:overflow-x-hidden xl:overflow-y-auto"
            onTouchStart={handleTouchStart}
            onTouchEnd={handleTouchEnd}
          >
            <div
              className="flex transition-transform duration-500 ease-[cubic-bezier(0.32,0.72,0.24,1)] motion-reduce:transition-none"
              style={{ transform: `translateX(-${safePage * 100}%)` }}
            >
              {pages.map((pageMatches, pageIndex) => (
                <div
                  key={pageIndex}
                  className="w-full shrink-0 space-y-3 px-0.5"
                  aria-hidden={pageIndex !== safePage}
                  {...(pageIndex !== safePage ? { inert: true } : {})}
                >
                  {pageMatches.map((match) => (
                    <MatchCard key={match.id} match={match} />
                  ))}
                </div>
              ))}
            </div>
          </div>

          {pageCount > 1 && (
            <div className="mt-3 flex shrink-0 items-center justify-between gap-2">
              <button
                type="button"
                onClick={() => goTo(safePage - 1)}
                disabled={safePage === 0}
                aria-label="Previous fixtures"
                className="inline-flex size-7 shrink-0 items-center justify-center rounded-full border border-border bg-card/80 text-foreground transition-colors hover:bg-muted disabled:cursor-not-allowed disabled:opacity-40"
              >
                <ChevronLeft className="size-4" aria-hidden />
              </button>

              {pageCount <= MAX_DOT_PAGES ? (
                <div
                  className="flex min-w-0 flex-wrap items-center justify-center gap-1.5"
                  role="tablist"
                  aria-label="Fixture pages"
                >
                  {pages.map((_, i) => (
                    <button
                      key={i}
                      type="button"
                      role="tab"
                      aria-selected={i === safePage}
                      aria-label={`Page ${i + 1} of ${pageCount}`}
                      onClick={() => goTo(i)}
                      className={cn(
                        "cursor-pointer rounded-full transition-all duration-300",
                        i === safePage
                          ? "h-1.5 w-5 bg-pitch"
                          : "h-1.5 w-1.5 bg-border hover:bg-muted-foreground/50"
                      )}
                    />
                  ))}
                </div>
              ) : (
                <span className="text-[11px] tabular-nums text-muted-foreground">
                  Page {safePage + 1} / {pageCount}
                </span>
              )}

              <button
                type="button"
                onClick={() => goTo(safePage + 1)}
                disabled={safePage === pageCount - 1}
                aria-label="Next fixtures"
                className="inline-flex size-7 shrink-0 items-center justify-center rounded-full border border-border bg-card/80 text-foreground transition-colors hover:bg-muted disabled:cursor-not-allowed disabled:opacity-40"
              >
                <ChevronRight className="size-4" aria-hidden />
              </button>
            </div>
          )}
        </>
      )}

      <p className="mt-3 shrink-0 border-t border-border pt-3 text-center">
        <Link
          href="/matchweek"
          className="text-xs font-medium text-primary hover:underline"
        >
          Full matchweek board →
        </Link>
      </p>
    </Panel>
  );
}
