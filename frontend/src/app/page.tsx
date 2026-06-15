import { BanterFeedPanel } from "@/components/home/BanterFeedPanel";
import { HomeQuickNav } from "@/components/home/HomeQuickNav";
import { HomeStatsBar } from "@/components/home/HomeStatsBar";
import { HomeWelcomePanel } from "@/components/home/HomeWelcomePanel";
import { PredictionCenter } from "@/components/home/PredictionCenter";
import { RankingsPanel } from "@/components/home/RankingsPanel";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";

export default function HomePage() {
  return (
    <PageWithSideAds>
      <HomeWelcomePanel />
      <HomeStatsBar />
      <HomeQuickNav />

      <div className="grid grid-cols-1 gap-5 xl:grid-cols-12 xl:gap-4">
        <div id="predictions" className="scroll-mt-20 xl:col-span-4">
          <p className="home-section-label">Lock in</p>
          <PredictionCenter />
        </div>

        <div id="banter-feed" className="scroll-mt-20 xl:col-span-4">
          <p className="home-section-label">Watch the chaos</p>
          <BanterFeedPanel />
        </div>

        <div id="rankings" className="scroll-mt-20 xl:col-span-4">
          <p className="home-section-label">Ball knowledge board</p>
          <RankingsPanel />
        </div>
      </div>
    </PageWithSideAds>
  );
}
