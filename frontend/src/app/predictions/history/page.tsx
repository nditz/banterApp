"use client";

import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { PredictionReactionCard } from "@/components/PredictionReactionCard";
import { awardPostMatchAura } from "@/lib/aura";
import { useBanterMode } from "@/hooks/useBanterMode";
import { usePredictionHistory } from "@/hooks/usePredictions";
import {
  formatPickLabel,
  getFinishedMatchReaction,
  isUnderdogPick,
} from "@/lib/predictionReactions";
import { pickToOutcome, resolveMatchResult } from "@/lib/postMatchResults";
import { useEffect } from "react";

const typeLabels: Record<string, string> = {
  result: "Match Result",
  correct_score: "Correct Score",
  double_chance: "Double Chance",
};

const resultLabels: Record<string, string> = {
  home: "Home Win",
  draw: "Draw",
  away: "Away Win",
  home_or_draw: "Home or Draw",
  away_or_draw: "Away or Draw",
  home_or_away: "Home or Away",
};

function formatDate(iso: string): string {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(new Date(iso));
}

export default function PredictionHistoryPage() {
  const { data: predictions, isLoading, isError } = usePredictionHistory();
  const banterMode = useBanterMode();

  useEffect(() => {
    if (!predictions?.length) return;

    for (const prediction of predictions) {
      const result = resolveMatchResult(prediction);
      if (!result) continue;

      const teamA = prediction.match?.teamA ?? "Team A";
      const teamB = prediction.match?.teamB ?? "Team B";
      const userPick = pickToOutcome(prediction.predictionType, prediction.predictionValue);
      const underdog =
        userPick != null
          ? isUnderdogPick(prediction.matchId, teamA, teamB, userPick)
          : false;

      const reaction = getFinishedMatchReaction(prediction, banterMode, underdog);
      if (!reaction) continue;

      awardPostMatchAura(prediction.id, reaction.auraDelta);
    }
  }, [predictions, banterMode]);

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-xl font-semibold sm:text-2xl">Prediction History</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          All your picks in one place. Export a{" "}
          <Link href="/" className="font-medium text-primary hover:underline">
            cumulative post-match script
          </Link>{" "}
          from the homepage Content Studio.
        </p>
      </div>

      {isError && (
        <p className="text-sm text-muted-foreground">Showing demo history</p>
      )}

      {isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-28 w-full rounded-md" />
          ))}
        </div>
      ) : (
        <div className="space-y-4">
          {predictions?.map((prediction) => {
            const teamA = prediction.match?.teamA ?? "Team A";
            const teamB = prediction.match?.teamB ?? "Team B";
            const userPick = pickToOutcome(
              prediction.predictionType,
              prediction.predictionValue
            );
            const underdog =
              userPick != null
                ? isUnderdogPick(prediction.matchId, teamA, teamB, userPick)
                : false;
            const postMatchReaction = getFinishedMatchReaction(
              prediction,
              banterMode,
              underdog
            );
            const matchResult = resolveMatchResult(prediction);

            return (
              <Card key={prediction.id} className="border-border shadow-sm">
                <CardHeader className="pb-2">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <CardTitle className="text-base">
                        {prediction.match
                          ? `${prediction.match.teamA} vs ${prediction.match.teamB}`
                          : `Match ${prediction.matchId}`}
                      </CardTitle>
                      <CardDescription>
                        {formatDate(prediction.createdAt)}
                      </CardDescription>
                    </div>
                    <Badge
                      variant={
                        (prediction.pointsAwarded ?? 0) > 0 ? "default" : "secondary"
                      }
                    >
                      +{prediction.pointsAwarded ?? 0} pts
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent className="space-y-3">
                  <p className="text-sm">
                    <span className="text-muted-foreground">
                      {typeLabels[prediction.predictionType] ??
                        prediction.predictionType}
                      :{" "}
                    </span>
                    <span className="font-medium">
                      {resultLabels[prediction.predictionValue] ??
                        formatPickLabel(
                          prediction.predictionType,
                          prediction.predictionValue,
                          teamA,
                          teamB
                        )}
                    </span>
                  </p>

                  {matchResult && (
                    <p className="text-xs font-medium text-pitch">
                      Final: {matchResult.label}
                    </p>
                  )}

                  {postMatchReaction && (
                    <PredictionReactionCard reaction={postMatchReaction} />
                  )}
                </CardContent>
              </Card>
            );
          })}
          {predictions?.length === 0 && (
            <p className="py-12 text-center text-muted-foreground">
              No predictions yet. Head to the homepage to make your first pick!
            </p>
          )}
        </div>
      )}
    </div>
  );
}
