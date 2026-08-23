import type { Metadata } from "next";
import { MatchweekBoard } from "@/components/home/MatchweekBoard";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";

export const metadata: Metadata = {
  title: "Matchweek picks",
  description:
    "Predict every Premier League fixture this matchweek. Result, exact score, or double chance — lock in before kickoff.",
  alternates: { canonical: "/matchweek" },
};

export default function MatchweekPage() {
  return (
    <PageWithSideAds>
      <div className="mx-auto max-w-3xl space-y-5">
        <header>
          <p className="page-kicker">Premier League 2026/27</p>
          <h1 className="mt-3 text-2xl font-bold sm:text-3xl">Matchweek picks</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Lock every fixture this week. Result +3, exact score +7, double chance +2. Nail the
            lot and take the perfect matchweek bonus.
          </p>
        </header>
        <MatchweekBoard />
      </div>
    </PageWithSideAds>
  );
}
