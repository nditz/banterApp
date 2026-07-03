"use client";

import Link from "next/link";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";
import { PredictionLockBanner } from "@/components/predictions/PredictionLockBanner";
import { PlayerPredictionForm } from "@/components/predictions/PlayerSelector";
import { useUserPredictions } from "@/hooks/useUserPredictions";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export default function TopScorerPredictionPage() {
  const { data: status, refetch } = useUserPredictions();
  const category = status?.categories.find((c) => c.predictionType === "top_goal_scorer");

  return (
    <PageWithSideAds>
      <div className="mx-auto max-w-2xl space-y-5">
        <Link href="/predictions" className={cn(buttonVariants({ variant: "ghost", size: "sm" }))}>
          ← All predictions
        </Link>
        {status && (
          <PredictionLockBanner isLocked={status.isLocked} lockDeadline={status.lockDeadline} />
        )}
        <PlayerPredictionForm
          predictionType="top_goal_scorer"
          label="Top goal scorer"
          description="Who finishes as the tournament's leading scorer?"
          existingId={category?.pick?.id}
          existingPlayerId={category?.pick?.playerId}
          isLocked={status?.isLocked ?? false}
          canEdit={status?.canEdit ?? false}
          onSaved={() => refetch()}
        />
      </div>
    </PageWithSideAds>
  );
}
