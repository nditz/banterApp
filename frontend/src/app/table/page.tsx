import type { Metadata } from "next";
import { LeagueTable } from "@/components/home/LeagueTable";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";

export const metadata: Metadata = {
  title: "Premier League table",
  description: "Live Premier League standings — played, goal difference, and points.",
  alternates: { canonical: "/table" },
};

export default function TablePage() {
  return (
    <PageWithSideAds>
      <div className="mx-auto max-w-3xl space-y-5">
        <header>
          <p className="page-kicker">2026/27 standings</p>
          <h1 className="mt-3 text-2xl font-bold sm:text-3xl">Premier League table</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            The 2026/27 table. Use it when you call the title, top four, and relegation.
          </p>
        </header>
        <LeagueTable />
      </div>
    </PageWithSideAds>
  );
}
