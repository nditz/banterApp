"use client";

import Link from "next/link";
import { Clapperboard, History, Mic2, Sparkles, Table2, Trophy } from "lucide-react";
import { RankingsPanel } from "@/components/home/RankingsPanel";
import { buttonVariants } from "@/components/ui/button";
import { useSupabaseUser } from "@/hooks/useSupabaseUser";
import { cn } from "@/lib/utils";

const links = [
  {
    href: "/predictions/history",
    label: "History",
    description: "Your picks and receipts",
    icon: History,
  },
  {
    href: "/awards",
    label: "Season Calls",
    description: "Title, Golden Boot and more",
    icon: Sparkles,
  },
  {
    href: "/table",
    label: "Table",
    description: "Premier League standings",
    icon: Table2,
  },
  {
    href: "/pundits",
    label: "Pundits",
    description: "Follow desks and compare takes",
    icon: Mic2,
  },
  {
    href: "/studio",
    label: "Studio",
    description: "Turn receipts into scripts",
    icon: Clapperboard,
  },
] as const;

export function MeHub() {
  const { isSignedIn, displayName, email } = useSupabaseUser();
  const greeting = displayName || email?.split("@")[0] || null;

  return (
    <div className="mx-auto max-w-4xl space-y-5">
      <header>
        <p className="page-kicker">Your board</p>
        <h1 className="mt-3 text-2xl font-bold sm:text-3xl">Me</h1>
        <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
          {isSignedIn && greeting
            ? `Signed in as ${greeting}. History, season calls and Aura live here.`
            : "Guest play is saved to this device. History, season calls and Aura still count."}
        </p>
        {!isSignedIn && (
          <div className="mt-3 flex flex-wrap gap-2">
            <Link href="/auth/login" className={cn(buttonVariants({ variant: "outline", size: "sm" }), "h-8 text-xs")}>
              Log in
            </Link>
            <Link href="/auth/register" className={cn(buttonVariants({ size: "sm" }), "btn-tournament h-8 text-xs")}>
              Join free
            </Link>
          </div>
        )}
      </header>

      <nav aria-label="Personal shortcuts" className="grid gap-2 sm:grid-cols-2">
        {links.map(({ href, label, description, icon: Icon }) => (
          <Link
            key={href}
            href={href}
            className="flex items-start gap-3 rounded-md border border-border bg-card px-4 py-3 shadow-sm transition-colors hover:bg-muted/40"
          >
            <span className="mt-0.5 inline-flex size-8 items-center justify-center rounded-md bg-muted">
              <Icon className="size-4" aria-hidden />
            </span>
            <span>
              <span className="block text-sm font-semibold">{label}</span>
              <span className="text-xs text-muted-foreground">{description}</span>
            </span>
          </Link>
        ))}
      </nav>

      <section>
        <p className="home-section-label">Aura</p>
        <RankingsPanel />
      </section>

      <p className="flex items-center gap-2 text-xs text-muted-foreground">
        <Trophy className="size-3.5" aria-hidden />
        <Link href="/rules" className="font-semibold text-foreground hover:underline">
          Scoring rules
        </Link>
        {" · "}
        private leagues live under Leagues.
      </p>
    </div>
  );
}
