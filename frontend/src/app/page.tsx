import type { Metadata } from "next";
import { BanterFeedPanel } from "@/components/home/BanterFeedPanel";
import { HomePunditStrip } from "@/components/home/HomePunditStrip";
import { HomeQuickNav } from "@/components/home/HomeQuickNav";
import { HomeReturningPanel } from "@/components/home/HomeReturningPanel";
import { HomeStatsBar } from "@/components/home/HomeStatsBar";
import { HomeStudioDemo } from "@/components/home/HomeStudioDemo";
import { HomeWelcomePanel } from "@/components/home/HomeWelcomePanel";
import { LeagueTable } from "@/components/home/LeagueTable";
import { PredictionCenter } from "@/components/home/PredictionCenter";
import { RankingsPanel } from "@/components/home/RankingsPanel";
import { cn } from "@/lib/utils";

export const metadata: Metadata = {
  title: {
    absolute: "Ball Takes — Premier League Predictions, Banter & Aura Rankings",
  },
  description:
    "Predict the Premier League. Beat your mates. Come back next matchweek. Lock picks, compete in private leagues and climb the aura rankings — no signup required.",
  alternates: { canonical: "/" },
};

const stickySideClass =
  "scroll-mt-14 lg:sticky lg:top-14 lg:z-10 lg:self-start xl:sticky xl:top-14";

const stickyScrollSideClass = cn(
  stickySideClass,
  "lg:max-h-[calc(100vh-3.5rem)] lg:overflow-y-auto lg:overscroll-y-contain lg:pr-0.5"
);

export default function HomePage() {
  return (
    <>
      <HomeWelcomePanel />
      <HomeStatsBar />
      <HomeReturningPanel />
      <HomeQuickNav />

      <div className="grid grid-cols-1 items-start gap-5 lg:grid-cols-12 lg:gap-4">
        <div id="banter-feed" className="scroll-mt-14 min-w-0 lg:col-span-7">
          <p className="home-section-label">Live takes</p>
          <BanterFeedPanel />
        </div>

        <div id="predictions" className={cn(stickySideClass, "min-w-0 lg:col-span-5")}>
          <p className="home-section-label">Make a pick</p>
          <PredictionCenter />
        </div>
      </div>

      <div className="mt-5 min-w-0 space-y-5">
        <HomePunditStrip />
        <HomeStudioDemo />
      </div>

      <div
        id="rankings"
        className={cn(stickyScrollSideClass, "mt-5 min-w-0")}
      >
        <p className="home-section-label">Ball takes board</p>
        <div className="space-y-4">
          <LeagueTable compact />
          <RankingsPanel />
        </div>
      </div>
    </>
  );
}
