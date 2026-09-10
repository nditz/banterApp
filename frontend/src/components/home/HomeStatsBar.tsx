"use client";

import { useMemo, type ReactNode } from "react";
import Link from "next/link";
import { CalendarDays, Sparkles, Users } from "lucide-react";
import { ErrorState } from "@/components/ui/states";
import { Skeleton } from "@/components/ui/skeleton";
import { useAura } from "@/hooks/useAura";
import { useCurrentMatchweek, useMatches } from "@/hooks/useMatches";
import { useMyLeagues } from "@/hooks/useLeaderboard";
import { useSession } from "@/hooks/useSession";
import { useSupabaseUser } from "@/hooks/useSupabaseUser";
import { isMatchLocked } from "@/lib/anonymous";
import { cn } from "@/lib/utils";

function formatCount(value: number): string {
  return new Intl.NumberFormat("en-GB").format(value);
}

interface StatItemProps {
  icon: ReactNode;
  label: string;
  value: string;
  hint?: string;
  href?: string;
  accent?: "gold" | "pitch" | "flare";
}

const accentIcon: Record<NonNullable<StatItemProps["accent"]>, string> = {
  gold: "text-gold",
  pitch: "text-pitch",
  flare: "text-flare",
};

function StatItem({ icon, label, value, hint, href, accent = "gold" }: StatItemProps) {
  const content = (
    <>
      <span className={cn("flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted/60", accentIcon[accent])}>
        {icon}
      </span>
      <div className="min-w-0">
        <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
          {label}
        </p>
        <p className="font-display text-lg font-semibold leading-none text-foreground">
          {value}
        </p>
        {hint && (
          <p className="mt-0.5 truncate text-[10px] text-muted-foreground">{hint}</p>
        )}
      </div>
    </>
  );

  if (href) {
    return (
      <Link href={href} className="home-stat-card flex cursor-pointer items-center gap-3">
        {content}
      </Link>
    );
  }

  return <div className="home-stat-card flex items-center gap-3">{content}</div>;
}

export function HomeStatsBar() {
  const { data: matches, isLoading: matchesLoading } = useMatches();
  const { data: currentWeek, isLoading: weekLoading } = useCurrentMatchweek();
  const { data: myLeagues, isLoading: leaguesLoading, isError: leaguesError } = useMyLeagues();
  const { summary: aura, isLoading: auraLoading, isError: auraError } = useAura();
  const { data: session, isLoading: sessionLoading } = useSession();
  const { isSignedIn } = useSupabaseUser();

  const signedIn = isSignedIn || Boolean(session?.authenticated && !session.anonymous);
  const openFixtures = useMemo(
    () => (currentWeek?.matches ?? matches ?? []).filter((m) => !isMatchLocked(m)).length,
    [currentWeek?.matches, matches]
  );
  const fixturesLoading = weekLoading && matchesLoading;
  const leagueCount = myLeagues?.leagues.length ?? 0;
  const matchweekLabel = currentWeek?.number ? `MW ${currentWeek.number}` : "Open picks";
  const hasSettledActivity = aura.settledPicks > 0;
  const hasAuraTotal = !auraError && aura.total > 0;
  const showAura = !auraLoading && hasAuraTotal;
  const showLeagues =
    Boolean(session?.termsAccepted) && !leaguesLoading && !leaguesError && leagueCount > 0;
  const showBar = hasSettledActivity || (signedIn && hasAuraTotal);

  const auraHint =
    aura.weeklyChange > 0
      ? `+${formatCount(aura.weeklyChange)} this week`
      : aura.streak > 1
        ? `${aura.streak} correct in a row`
        : "Earned from settled picks";

  if (!signedIn) {
    if (sessionLoading || auraLoading) return null;
    if (!hasSettledActivity) return null;
  } else if (sessionLoading || auraLoading) {
    return (
      <section className="mb-4 grid grid-cols-2 gap-2 sm:grid-cols-3 sm:gap-3" aria-label="Season at a glance">
        <Skeleton className="home-stat-card h-[4.5rem] w-full" />
        <Skeleton className="home-stat-card h-[4.5rem] w-full" />
      </section>
    );
  }

  if (!showBar) {
    return null;
  }

  return (
    <section
      className="mb-4 grid grid-cols-2 gap-2 sm:grid-cols-3 sm:gap-3"
      aria-label="Season at a glance"
    >
      {fixturesLoading ? (
        <Skeleton className="home-stat-card h-[4.5rem] w-full" />
      ) : (
        <StatItem
          icon={<CalendarDays className="size-4" aria-hidden />}
          label={matchweekLabel}
          value={formatCount(openFixtures)}
          hint="Fixtures still open"
          href="#predictions"
          accent="pitch"
        />
      )}
      {leaguesLoading ? (
        <Skeleton className="home-stat-card h-[4.5rem] w-full" />
      ) : showLeagues ? (
        <StatItem
          icon={<Users className="size-4" aria-hidden />}
          label="Your leagues"
          value={formatCount(leagueCount)}
          hint="Private standings"
          href="/leagues"
          accent="gold"
        />
      ) : null}
      {auraError ? (
        <div className="home-stat-card col-span-2 sm:col-span-1">
          <ErrorState
            dense
            title="Aura unavailable"
            description="Personal totals will return when the request succeeds."
          />
        </div>
      ) : showAura ? (
        <StatItem
          icon={<Sparkles className="size-4" aria-hidden />}
          label="Your aura"
          value={formatCount(aura.total)}
          hint={auraHint}
          accent="flare"
        />
      ) : null}
    </section>
  );
}
