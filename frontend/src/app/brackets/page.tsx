import { BracketBoard } from "@/components/brackets/BracketBoard";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";

export default function BracketsPage() {
  return (
    <PageWithSideAds>
      <div className="mx-auto max-w-[1200px]">
        <header className="mb-4">
          <h1 className="text-xl font-bold">Tournament bracket</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Predict the full World Cup path — group stage through the Final. Pick group winners to
            populate the knockout tree, then call the champion. Fixtures sync from API-Football when
            configured; flags show each nation.
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
