import type { Metadata } from "next";
import { TrackPageView } from "@/components/analytics/TrackPageView";
import { LeagueTable } from "@/components/home/LeagueTable";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";

export const metadata: Metadata = {
  title: "Premier League table",
  description: "Live Premier League standings ranked by points, goal difference, then goals scored.",
  alternates: { canonical: "/table" },
};

export default function TablePage() {
  return (
    <PageWithSideAds>
      <TrackPageView event="leaderboard_viewed" properties={{ scope: "competition" }} />
      <div className="mx-auto max-w-3xl space-y-5">
        <header>
          <p className="page-kicker">2026/27 standings</p>
          <h1 className="mt-3 text-2xl font-bold sm:text-3xl">Premier League table</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Ranked the Premier League way: points, then goal difference, then goals scored. Use it
            when you call the title, top four, and relegation.
          </p>
        </header>
        <LeagueTable />
      </div>
    </PageWithSideAds>
  );
}
