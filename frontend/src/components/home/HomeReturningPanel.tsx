"use client";

import Link from "next/link";
import { Clapperboard, Receipt, Target, Trophy } from "lucide-react";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { FreshnessBadge } from "@/components/ui/freshness-badge";
import { useAura } from "@/hooks/useAura";
import { useCurrentMatchweek } from "@/hooks/useMatches";
import { useReceipts } from "@/hooks/useReceipts";
import { useStudioStories } from "@/hooks/useStudioStories";
import { useSession } from "@/hooks/useSession";
import { useSupabaseUser } from "@/hooks/useSupabaseUser";
import { isMatchLocked } from "@/lib/anonymous";
import { stripHtml } from "@/lib/strip-html";

function formatKickoff(iso: string): string {
  const date = new Date(iso);
  if (!Number.isFinite(date.getTime())) return "";
  return new Intl.DateTimeFormat("en-GB", {
    weekday: "short",
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "Europe/London",
  }).format(date);
}

function formatCount(value: number): string {
  return new Intl.NumberFormat("en-GB").format(value);
}

export function HomeReturningPanel() {
  const { isSignedIn } = useSupabaseUser();
  const { data: session } = useSession();
  const signedIn = isSignedIn || Boolean(session?.authenticated && !session.anonymous);

  const matchweek = useCurrentMatchweek();
  const receipts = useReceipts();
  const stories = useStudioStories();
  const { summary: aura, isLoading: auraLoading, isError: auraError } = useAura();

  if (!signedIn) return null;

  const nextPick = [...(matchweek.data?.matches ?? [])]
    .filter((match) => !isMatchLocked(match))
    .sort((a, b) => new Date(a.kickoffTime).getTime() - new Date(b.kickoffTime).getTime())[0];

  const receiptRows = [...(receipts.data ?? [])].sort(
    (a, b) => new Date(b.settledAt).getTime() - new Date(a.settledAt).getTime()
  );
  const receiptCount = receiptRows.length;
  const latestReceipt = receiptRows[0];

  const studioCards = [
    ...(stories.data?.latestReceipts ?? []),
    ...(stories.data?.youVsPundits ?? []),
    ...(stories.data?.trending ?? []),
  ];
  const studioReady = studioCards.filter((card, index, all) => all.findIndex((row) => row.id === card.id) === index);

  const showRank = !auraError && aura.rank != null;
  const showWeekly = !auraError && aura.weeklyChange !== 0;

  const items: Array<"next" | "receipts" | "studio" | "aura"> = [];
  if (nextPick) items.push("next");
  if (receiptCount > 0) items.push("receipts");
  if (studioReady.length > 0) items.push("studio");
  if (showRank || showWeekly) items.push("aura");

  const receiptsLoading = receipts.isFetching;
  const loading =
    (matchweek.isLoading && !matchweek.data) ||
    receiptsLoading ||
    (stories.isLoading && !stories.data) ||
    auraLoading;

  if (loading && items.length === 0) {
    return (
      <section className="mb-4" aria-label="Your week">
        <Skeleton className="h-24 w-full rounded-xl" />
      </section>
    );
  }

  if (items.length === 0) return null;

  const refreshing =
    (Boolean(matchweek.data) && matchweek.isFetching) ||
    (receiptCount > 0 && receipts.isFetching) ||
    (studioReady.length > 0 && stories.isFetching);

  const latestReceiptLabel = latestReceipt?.match
    ? `${latestReceipt.match.teamA} vs ${latestReceipt.match.teamB}`
    : latestReceipt
      ? "Latest settled pick"
      : null;
  const studioTitle = stripHtml(studioReady[0]?.title ?? "");

  return (
    <Panel
      title="Your week"
      subtitle="From your picks, receipts, and Studio — nothing invented"
      accent="pitch"
      className="mb-4"
      action={
        refreshing ? <FreshnessBadge status="stale" label="Refreshing" /> : null
      }
    >
      <ul className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        {nextPick ? (
          <li>
            <Link
              href="#predictions"
              className="flex min-w-0 items-start gap-3 rounded-lg border border-border bg-background/70 px-3 py-2.5 hover:bg-muted/50"
            >
              <Target className="mt-0.5 size-4 shrink-0 text-pitch" aria-hidden />
              <div className="min-w-0">
                <p className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                  Next pick
                </p>
                <p className="truncate text-sm font-semibold">
                  {nextPick.teamA} vs {nextPick.teamB}
                </p>
                <p className="truncate text-[11px] text-muted-foreground">
                  {formatKickoff(nextPick.kickoffTime)}
                </p>
              </div>
            </Link>
          </li>
        ) : null}

        {receiptCount > 0 ? (
          <li>
            <Link
              href="/predictions/history"
              className="flex min-w-0 items-start gap-3 rounded-lg border border-border bg-background/70 px-3 py-2.5 hover:bg-muted/50"
            >
              <Receipt className="mt-0.5 size-4 shrink-0 text-gold" aria-hidden />
              <div className="min-w-0">
                <p className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                  New receipts
                </p>
                <p className="text-sm font-semibold">
                  {formatCount(receiptCount)} settled
                </p>
                {latestReceiptLabel ? (
                  <p className="truncate text-[11px] text-muted-foreground">{latestReceiptLabel}</p>
                ) : null}
              </div>
            </Link>
          </li>
        ) : null}

        {studioReady.length > 0 ? (
          <li>
            <Link
              href="/studio"
              className="flex min-w-0 items-start gap-3 rounded-lg border border-border bg-background/70 px-3 py-2.5 hover:bg-muted/50"
            >
              <Clapperboard className="mt-0.5 size-4 shrink-0 text-flare" aria-hidden />
              <div className="min-w-0">
                <p className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                  Studio-ready
                </p>
                <p className="text-sm font-semibold">
                  {formatCount(studioReady.length)} stor{studioReady.length === 1 ? "y" : "ies"}
                </p>
                {studioTitle ? (
                  <p className="truncate text-[11px] text-muted-foreground">{studioTitle}</p>
                ) : null}
              </div>
            </Link>
          </li>
        ) : null}

        {showRank || showWeekly ? (
          <li>
            <Link
              href="#rankings"
              className="flex min-w-0 items-start gap-3 rounded-lg border border-border bg-background/70 px-3 py-2.5 hover:bg-muted/50"
            >
              <Trophy className="mt-0.5 size-4 shrink-0 text-gold" aria-hidden />
              <div className="min-w-0">
                <p className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                  League movement
                </p>
                <p className="text-sm font-semibold">
                  {showRank && aura.rank != null ? `Rank ${formatCount(aura.rank)}` : "Aura this week"}
                </p>
                <p className="truncate text-[11px] text-muted-foreground">
                  {showWeekly
                    ? `${aura.weeklyChange > 0 ? "+" : ""}${formatCount(aura.weeklyChange)} this week`
                    : aura.percentile != null
                      ? `Top ${Math.round(aura.percentile)}%`
                      : "From settled picks"}
                </p>
              </div>
            </Link>
          </li>
        ) : null}
      </ul>
    </Panel>
  );
}
