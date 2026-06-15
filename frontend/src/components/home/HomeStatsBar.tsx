"use client";

import { useMemo, type ReactNode } from "react";
import Link from "next/link";
import { CalendarDays, Shield, Sparkles, Users } from "lucide-react";
import { useAura } from "@/hooks/useAura";
import { useMatches } from "@/hooks/useMatches";
import { useMyLeagues } from "@/hooks/useLeaderboard";
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
  const { data: matches } = useMatches();
  const { data: myLeagues } = useMyLeagues();
  const { aura } = useAura();

  const openFixtures = useMemo(
    () => (matches ?? []).filter((m) => !isMatchLocked(m)).length,
    [matches]
  );
  const leagueCount = myLeagues?.leagues.length ?? 0;

  return (
    <section
      className="mb-4 grid grid-cols-2 gap-2 sm:grid-cols-4 sm:gap-3"
      aria-label="Tournament at a glance"
    >
      <StatItem
        icon={<CalendarDays className="size-4" aria-hidden />}
        label="Open picks"
        value={formatCount(openFixtures)}
        hint="Fixtures still open"
        href="#predictions"
        accent="pitch"
      />
      <StatItem
        icon={<Users className="size-4" aria-hidden />}
        label="Your leagues"
        value={formatCount(leagueCount)}
        hint={leagueCount === 0 ? "Join or create one" : "Private standings"}
        href="/leagues"
        accent="gold"
      />
      <StatItem
        icon={<Sparkles className="size-4" aria-hidden />}
        label="Your aura"
        value={formatCount(aura)}
        hint="Ball knowledge points"
        accent="flare"
      />
      <StatItem
        icon={<Shield className="size-4" aria-hidden />}
        label="Always free"
        value="£0"
        hint="No wagering — banter only"
        accent="gold"
      />
    </section>
  );
}
