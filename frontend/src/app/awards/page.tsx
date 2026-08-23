import type { Metadata } from "next";
import { TournamentBonusBoard } from "@/components/bonuses/TournamentBonusBoard";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";
import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";
import { TOURNAMENT_BONUS_RULES } from "@/lib/scoring-rules";
import { cn } from "@/lib/utils";

export const metadata: Metadata = {
  title: "Season awards",
  description:
    "Lock Premier League season awards — title, top four, relegation, Golden Boot and more — on Ball Takes.",
  alternates: { canonical: "/awards" },
};

export default function AwardsPage() {
  return (
    <PageWithSideAds>
      <div className="mx-auto max-w-4xl space-y-5">
        <header>
          <p className="page-kicker">Lock before kickoff</p>
          <h1 className="mt-3 text-2xl font-bold sm:text-3xl">Season awards</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Call the title, top four, the drop, and the individual awards before the first
            kickoff. Nail them in a private league with at least 3 members and swing the
            standings.
          </p>
        </header>

        <TournamentBonusBoard />

        <section className="rounded-md border border-border bg-card p-4 shadow-sm sm:p-5">
          <h2 className="text-base font-semibold">Scoring by difficulty</h2>
          <p className="mt-1 text-xs text-muted-foreground">
            Harder calls pay more. Points are awarded when official season awards are announced.
          </p>
          <ul className="mt-4 space-y-2">
            {TOURNAMENT_BONUS_RULES.map((rule) => (
              <li
                key={rule.id}
                className="flex items-center justify-between gap-3 rounded-md border border-border bg-muted/20 px-3 py-2 text-sm"
              >
                <div>
                  <span className="font-medium">{rule.title}</span>
                  <span className="ml-2 text-[10px] font-bold uppercase tracking-wide text-muted-foreground">
                    {rule.difficulty}
                  </span>
                </div>
                <span className="shrink-0 font-bold text-gold">+{rule.points}</span>
              </li>
            ))}
          </ul>
          <Link
            href="/rules"
            className={cn(buttonVariants({ variant: "outline", size: "sm" }), "mt-4 h-8 text-xs")}
          >
            Full rules & scoring
          </Link>
        </section>
      </div>
    </PageWithSideAds>
  );
}
