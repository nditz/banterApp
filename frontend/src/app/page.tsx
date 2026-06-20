import { BanterFeedPanel } from "@/components/home/BanterFeedPanel";
import { HomeQuickNav } from "@/components/home/HomeQuickNav";
import { HomeStatsBar } from "@/components/home/HomeStatsBar";
import { HomeWelcomePanel } from "@/components/home/HomeWelcomePanel";
import { PredictionCenter } from "@/components/home/PredictionCenter";
import { RankingsPanel } from "@/components/home/RankingsPanel";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";
import { cn } from "@/lib/utils";

const stickySideClass =
  "scroll-mt-14 xl:sticky xl:top-14 xl:z-10 xl:self-start";

const stickyScrollSideClass = cn(
  stickySideClass,
  "xl:max-h-[calc(100vh-3.5rem)] xl:overflow-y-auto xl:overscroll-y-contain xl:pr-0.5"
);

export default function HomePage() {
  return (
    <PageWithSideAds>
      <HomeWelcomePanel />
      <HomeStatsBar />
      <HomeQuickNav />

      <div className="grid grid-cols-1 items-start gap-5 xl:grid-cols-12 xl:gap-4">
        <div id="predictions" className={cn(stickySideClass, "xl:col-span-4")}>
          <p className="home-section-label">Lock in</p>
          <PredictionCenter />
        </div>

        <div id="banter-feed" className="scroll-mt-14 xl:col-span-4">
          <p className="home-section-label">Watch the chaos</p>
          <BanterFeedPanel />
        </div>

        <div id="rankings" className={cn(stickyScrollSideClass, "xl:col-span-4")}>
          <p className="home-section-label">Ball takes board</p>
          <RankingsPanel />
        </div>
      </div>
    </PageWithSideAds>
  );
}
