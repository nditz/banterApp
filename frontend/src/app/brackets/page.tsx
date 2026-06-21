import { BracketBoard } from "@/components/brackets/BracketBoard";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";

export default function BracketsPage() {
  return (
    <PageWithSideAds>
      <div className="mx-auto max-w-[1200px]">
        <header className="mb-4">
          <h1 className="text-xl font-bold sm:text-2xl">Tournament bracket</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Predict the full 2026 World Cup path — 12 groups, Round of 32, through the Final. Pick
            group results to populate the knockout tree, then call the champion. Fixtures sync from
            OpenFootball when live data is enabled.
          </p>
        </header>

        <div className="content-panel">
          <div className="content-panel-header">
            <h2 className="text-sm font-semibold text-brand-foreground">Your bracket</h2>
          </div>
          <div className="p-4">
            <BracketBoard />
          </div>
        </div>
      </div>
    </PageWithSideAds>
  );
}
