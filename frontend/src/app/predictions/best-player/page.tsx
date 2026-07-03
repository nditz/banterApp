"use client";

import Link from "next/link";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";
import { PredictionLockBanner } from "@/components/predictions/PredictionLockBanner";
import { PlayerPredictionForm } from "@/components/predictions/PlayerSelector";
import { useUserPredictions } from "@/hooks/useUserPredictions";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export default function BestPlayerPredictionPage() {
  const { data: status, refetch } = useUserPredictions();
  const bestPlayer = status?.categories.find((c) => c.predictionType === "best_player");
  const pot = status?.categories.find((c) => c.predictionType === "player_of_tournament");
  const young = status?.categories.find((c) => c.predictionType === "best_young_player");
  const goldenBoot = status?.categories.find((c) => c.predictionType === "golden_boot");

  return (
    <PageWithSideAds>
      <div className="mx-auto max-w-2xl space-y-8">
        <Link href="/predictions" className={cn(buttonVariants({ variant: "ghost", size: "sm" }))}>
          ← All predictions
        </Link>
        {status && (
          <PredictionLockBanner isLocked={status.isLocked} lockDeadline={status.lockDeadline} />
        )}

        <PlayerPredictionForm
          predictionType="best_player"
          label="Best player"
          description="Your pick for the best overall player."
          existingId={bestPlayer?.pick?.id}
          existingPlayerId={bestPlayer?.pick?.playerId}
          isLocked={status?.isLocked ?? false}
          canEdit={status?.canEdit ?? false}
          onSaved={() => refetch()}
        />

        <PlayerPredictionForm
          predictionType="player_of_tournament"
          label="Player of the tournament"
          description="Official-style player of the tournament pick."
          existingId={pot?.pick?.id}
          existingPlayerId={pot?.pick?.playerId}
          isLocked={status?.isLocked ?? false}
          canEdit={status?.canEdit ?? false}
          onSaved={() => refetch()}
        />

        <PlayerPredictionForm
          predictionType="golden_boot"
          label="Golden Boot"
          description="Your Golden Boot winner."
          existingId={goldenBoot?.pick?.id}
          existingPlayerId={goldenBoot?.pick?.playerId}
          isLocked={status?.isLocked ?? false}
          canEdit={status?.canEdit ?? false}
          onSaved={() => refetch()}
        />

        <PlayerPredictionForm
          predictionType="best_young_player"
          label="Best young player"
          description="Who shines as the best young talent?"
          existingId={young?.pick?.id}
          existingPlayerId={young?.pick?.playerId}
          isLocked={status?.isLocked ?? false}
          canEdit={status?.canEdit ?? false}
          onSaved={() => refetch()}
        />
      </div>
    </PageWithSideAds>
  );
}
